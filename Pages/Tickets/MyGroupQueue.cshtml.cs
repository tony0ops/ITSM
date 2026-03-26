using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;

        public MyGroupQueueModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Ticket> Tickets { get; set; } = [];
        public string GroupName { get; set; } = "Your Group";
        public string? TeamName { get; set; }
        public int AtRiskCount { get; set; }
        public int BreachedCount { get; set; }
        public int InProgressCount { get; set; }
        public bool HasTeamAssignment { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var profile = await _context.UserProfiles
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile?.Team == null)
            {
                // No team assigned — show empty queue with message
                HasTeamAssignment = false;
                return Page();
            }

            HasTeamAssignment = true;
            var userGroup = profile.Team.Group;
            GroupName = profile.Team.Group.ToString().Replace("_", " ");
            TeamName = profile.Team.Name;

            // Consistent rule: all open tickets assigned to the group
            Tickets = await _context.Tickets
                .Include(t => t.Customer)
                .Where(t => t.AssignedGroup == userGroup &&
                           t.Status != TicketStatus.Closed &&
                           t.Status != TicketStatus.Cancelled &&
                           t.Status != TicketStatus.Resolved)
                .OrderBy(t => t.SlaDueAt)
                .ToListAsync();

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