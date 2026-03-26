using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }
        public Dictionary<TicketStatus, int> TicketsByStatus { get; set; } = new();
        public Dictionary<Severity, int> TicketsBySeverity { get; set; } = new();
        public Dictionary<string, int> TicketsByCustomer { get; set; } = new();
        public double AverageResolutionHours { get; set; }
        public double SlaComplianceRate { get; set; }
        public int ResolvedTicketCount { get; set; }

        // Per-severity resolution averages
        public double AvgResolutionCritical { get; set; }
        public double AvgResolutionHigh { get; set; }
        public double AvgResolutionMedium { get; set; }
        public double AvgResolutionLow { get; set; }

        // Customer filter (persisted via cookie)
        [BindProperty(SupportsGet = true)]
        public int? CustomerId { get; set; }

        public SelectList CustomerList { get; set; } = default!;
        public string? SelectedCustomerName { get; set; }

        private const string CustomerCookieKey = "Dashboard_CustomerId";

        public async Task OnGetAsync()
        {
            // Build customer dropdown
            CustomerList = new SelectList(
                await _context.Customers
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .Select(c => new { c.Id, c.CompanyName })
                    .ToListAsync(),
                "Id", "CompanyName");

            // Restore filter from cookie if not explicitly provided
            if (!CustomerId.HasValue &&
                Request.Cookies.TryGetValue(CustomerCookieKey, out var cookieVal) &&
                int.TryParse(cookieVal, out var savedId))
            {
                CustomerId = savedId;
            }

            // Persist or clear cookie
            if (CustomerId.HasValue)
            {
                Response.Cookies.Append(CustomerCookieKey, CustomerId.Value.ToString(),
                    new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30), HttpOnly = true, IsEssential = true });

                SelectedCustomerName = await _context.Customers
                    .Where(c => c.Id == CustomerId.Value)
                    .Select(c => c.CompanyName)
                    .FirstOrDefaultAsync();
            }
            else
            {
                Response.Cookies.Delete(CustomerCookieKey);
            }

            // Base query, optionally scoped to customer
            var query = _context.Tickets.AsQueryable();
            if (CustomerId.HasValue)
                query = query.Where(t => t.CustomerId == CustomerId.Value);

            TotalTickets = await query.CountAsync();

            OpenTickets = await query
                .Where(t => t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Cancelled)
                .CountAsync();

            ClosedTickets = await query
                .Where(t => t.Status == TicketStatus.Closed)
                .CountAsync();

            TicketsByStatus = await query
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            TicketsBySeverity = await query
                .GroupBy(t => t.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Severity, x => x.Count);

            TicketsByCustomer = await query
                .Where(t => t.Customer != null)
                .GroupBy(t => t.Customer!.CompanyName)
                .Select(g => new { Customer = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToDictionaryAsync(x => x.Customer, x => x.Count);

            // Resolution time — use ResolvedAt, fall back to ClosedAt
            var resolvedData = await query
                .Where(t => t.ResolvedAt.HasValue || t.ClosedAt.HasValue)
                .Select(t => new { t.CreatedAt, t.ResolvedAt, t.ClosedAt, t.Severity })
                .ToListAsync();

            ResolvedTicketCount = resolvedData.Count;

            if (resolvedData.Count > 0)
            {
                AverageResolutionHours = resolvedData
                    .Average(t => ((t.ResolvedAt ?? t.ClosedAt)!.Value - t.CreatedAt).TotalHours);

                double SeverityAvg(Severity sev)
                {
                    var subset = resolvedData.Where(t => t.Severity == sev).ToList();
                    return subset.Count > 0
                        ? subset.Average(t => ((t.ResolvedAt ?? t.ClosedAt)!.Value - t.CreatedAt).TotalHours)
                        : 0;
                }

                AvgResolutionCritical = SeverityAvg(Severity.Critical);
                AvgResolutionHigh = SeverityAvg(Severity.High);
                AvgResolutionMedium = SeverityAvg(Severity.Medium);
                AvgResolutionLow = SeverityAvg(Severity.Low);
            }

            // SLA compliance
            var now = DateTime.Now;
            var openList = await query
                .Where(t => t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Cancelled)
                .ToListAsync();

            var breachedCount = openList.Count(t => SlaPolicy.GetState(t, now) == SlaState.Breached);

            SlaComplianceRate = TotalTickets > 0
                ? ((TotalTickets - breachedCount) / (double)TotalTickets) * 100
                : 100;
        }

        public IActionResult OnGetClearFilter()
        {
            Response.Cookies.Delete(CustomerCookieKey);
            return RedirectToPage();
        }
    }
}