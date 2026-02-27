using Microsoft.AspNetCore.Mvc.RazorPages;
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

        // Summary stats
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }
        public Dictionary<TicketStatus, int> TicketsByStatus { get; set; } = new();
        public Dictionary<Severity, int> TicketsBySeverity { get; set; } = new();
        public Dictionary<string, int> TicketsByCustomer { get; set; } = new();
        public double AverageResolutionHours { get; set; }
        public double SlaComplianceRate { get; set; }

        public async Task OnGetAsync()
        {
            TotalTickets = await _context.Tickets.CountAsync();
            OpenTickets = await _context.Tickets
                .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved)
                .CountAsync();
            ClosedTickets = await _context.Tickets
                .Where(t => t.Status == TicketStatus.Closed)
                .CountAsync();

            // Group by status
            TicketsByStatus = await _context.Tickets
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Group by severity
            TicketsBySeverity = await _context.Tickets
                .GroupBy(t => t.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Severity, x => x.Count);

            // Top 5 customers by ticket count
            TicketsByCustomer = await _context.Tickets
                .Where(t => t.Customer != null)
                .GroupBy(t => t.Customer!.CompanyName)
                .Select(g => new { Customer = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToDictionaryAsync(x => x.Customer, x => x.Count);

            // Average resolution time
            var resolvedTickets = await _context.Tickets
                .Where(t => t.ResolvedAt.HasValue)
                .Select(t => new { t.CreatedAt, t.ResolvedAt })
                .ToListAsync();

            if (resolvedTickets.Any())
            {
                AverageResolutionHours = resolvedTickets
                    .Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours);
            }

            // SLA compliance (not breached)
            var now = DateTime.Now;
            var totalWithSla = await _context.Tickets.CountAsync();
            var breached = await _context.Tickets
                .Where(t => t.Status != TicketStatus.Closed)
                .ToListAsync();
            var breachedCount = breached.Count(t => SlaPolicy.GetState(t, now) == SlaState.Breached);

            SlaComplianceRate = totalWithSla > 0
                ? ((totalWithSla - breachedCount) / (double)totalWithSla) * 100
                : 100;
        }
    }
}