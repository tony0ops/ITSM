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

            // Get user's profile and team assignment
            var profile = await _context.UserProfiles
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            AssignmentGroup userGroup;

            if (profile?.Team != null)
            {
                userGroup = profile.Team.Group;
                GroupName = profile.Team.Group.ToString().Replace("_", " ");
                TeamName = profile.Team.Name;
                HasTeamAssignment = true;
            }
            else
            {
                userGroup = AssignmentGroup.ServiceDesk_L1;
                GroupName = "ServiceDesk L1 (Default)";
                HasTeamAssignment = false;
            }

            // Get all team member user IDs
            var teamMemberIds = new List<string>();
            if (profile?.TeamId != null)
            {
                teamMemberIds = await _context.UserProfiles
                    .Where(p => p.TeamId == profile.TeamId && p.IsActive)
                    .Select(p => p.UserId)
                    .ToListAsync();
            }

            // Show tickets assigned to the group OR assigned to any team member
            Tickets = await _context.Tickets
                .Include(t => t.Customer)
                .Where(t => (t.AssignedGroup == userGroup ||
                            (t.AssigneeUserId != null && teamMemberIds.Contains(t.AssigneeUserId))) &&
                           t.Status != TicketStatus.Closed &&
                           t.Status != TicketStatus.Cancelled &&
                           t.Status != TicketStatus.Resolved)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

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