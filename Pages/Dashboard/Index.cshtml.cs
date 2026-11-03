using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Dashboard
{
    public class IndexModel(ApplicationDbContext context) : PageModel
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int CompletedTickets { get; set; }
        public Dictionary<TicketStatus, int> TicketsByStatus { get; set; } = [];
        public Dictionary<Severity, int> TicketsBySeverity { get; set; } = [];
        public Dictionary<string, int> TicketsByCustomer { get; set; } = [];
        public double AverageResolutionHours { get; set; }
        public double SlaComplianceRate { get; set; }
        public int ResolvedTicketCount { get; set; }

        public double AvgResolutionCritical { get; set; }
        public double AvgResolutionHigh { get; set; }
        public double AvgResolutionMedium { get; set; }
        public double AvgResolutionLow { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CustomerId { get; set; }

        public SelectList CustomerList { get; set; } = default!;
        public string? SelectedCustomerName { get; set; }

        private const string CustomerCookieKey = "Dashboard_CustomerId";

        public async Task OnGetAsync()
        {
            CustomerList = new SelectList(
                await context.Customers
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .Select(c => new { c.Id, c.CompanyName })
                    .ToListAsync(),
                "Id", "CompanyName");

            // If the query string contains "customerId" explicitly (even empty),
            // treat it as a deliberate choice and do not restore the cookie.
            // This means selecting "All Customers" correctly clears the filter.
            var queryStringPresent = Request.Query.ContainsKey("customerId");

            if (!queryStringPresent)
            {
                // No query string at all - restore from cookie
                if (Request.Cookies.TryGetValue(CustomerCookieKey, out var cookieVal) &&
                    int.TryParse(cookieVal, out var savedId))
                {
                    CustomerId = savedId;
                }
            }

            // Persist or clear cookie based on resolved CustomerId
            if (CustomerId.HasValue)
            {
                Response.Cookies.Append(CustomerCookieKey, CustomerId.Value.ToString(),
                    new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30), HttpOnly = true, IsEssential = true });

                SelectedCustomerName = await context.Customers
                    .Where(c => c.Id == CustomerId.Value)
                    .Select(c => c.CompanyName)
                    .FirstOrDefaultAsync();
            }
            else
            {
                Response.Cookies.Delete(CustomerCookieKey);
            }

            var query = context.Tickets.AsQueryable();
            if (CustomerId.HasValue)
                query = query.Where(t => t.CustomerId == CustomerId.Value);

            TotalTickets = await query.CountAsync();

            OpenTickets = await query
                .Where(t => t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Cancelled)
                .CountAsync();

            CompletedTickets = await query
                .Where(t => t.Status == TicketStatus.Closed ||
                            t.Status == TicketStatus.Resolved ||
                            t.Status == TicketStatus.Cancelled)
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

            var resolvedData = await query
                .Where(t => t.Status != TicketStatus.Cancelled && (t.ResolvedAt.HasValue || t.ClosedAt.HasValue))
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
                AvgResolutionHigh     = SeverityAvg(Severity.High);
                AvgResolutionMedium   = SeverityAvg(Severity.Medium);
                AvgResolutionLow      = SeverityAvg(Severity.Low);
            }

            var now           = DateTime.Now;
            var openList      = await query
                .Where(t => t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Cancelled)
                .ToListAsync();

            var openCount     = openList.Count;
            var breachedCount = openList.Count(t => SlaPolicy.GetState(t, now) == SlaState.Breached);

            SlaComplianceRate = openCount > 0
                ? ((openCount - breachedCount) / (double)openCount) * 100
                : 100;
        }

        // Kept for backwards compatibility with any direct link, but the
        // dropdown now handles "All Customers" natively without it.
        public IActionResult OnGetClearFilter()
        {
            Response.Cookies.Delete(CustomerCookieKey);
            return RedirectToPage();
        }
    }
}