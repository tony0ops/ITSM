using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    public class MyGroupQueueModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyGroupQueueModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = [];
        public string GroupName { get; set; } = "Your Group";
        public int AtRiskCount { get; set; }
        public int BreachedCount { get; set; }
        public int InProgressCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Determine user's assignment group
            // For now, default to ServiceDesk_L1. In production, query from user profile or claims
            var userGroup = AssignmentGroup.ServiceDesk_L1;

            Tickets = await _context.Tickets
                .Where(t => t.AssignedGroup == userGroup && 
                           t.Status != TicketStatus.Closed && 
                           t.Status != TicketStatus.Cancelled &&
                           t.Status != TicketStatus.Resolved)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            GroupName = userGroup.ToString().Replace("_", " ");

            // Calculate statistics
            var now = DateTime.Now;
            AtRiskCount = Tickets.Count(t =>
            {
                var state = SlaPolicy.GetState(t, now);
                return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
            });

            BreachedCount = Tickets.Count(t => SlaPolicy.GetState(t, now) == SlaState.Breached);
            InProgressCount = Tickets.Count(t => t.Status == TicketStatus.InProgress);

            return Page();
        }

        public List<Ticket> GetAtRiskTickets()
        {
            var now = DateTime.Now;
            return Tickets
                .Where(t =>
                {
                    var state = SlaPolicy.GetState(t, now);
                    return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
                })
                .ToList();
        }

        public List<Ticket> GetBreachedTickets()
        {
            var now = DateTime.Now;
            return Tickets
                .Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached)
                .ToList();
        }

        public List<Ticket> GetInProgressTickets()
        {
            return Tickets
                .Where(t => t.Status == TicketStatus.InProgress)
                .ToList();
        }
    }
}