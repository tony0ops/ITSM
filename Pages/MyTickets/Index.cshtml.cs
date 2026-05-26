using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.MyTickets
{
    [Authorize]
    // IDE0290: primary constructor
    public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
    {
        // IDE0028: = [] instead of = new()
        public List<Ticket> AssignedToMe { get; set; } = [];
        public List<Ticket> MyTeamTickets { get; set; } = [];
        public string? TeamName { get; set; }
        public int TotalAssignedToMe { get; set; }
        public int TotalTeamTickets { get; set; }
        public int AtRiskCount { get; set; }
        public int BreachedCount { get; set; }
        public int CustomerRepliedCount { get; set; }

        public async Task OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return;

            AssignedToMe = await context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Comments)
                .Include(t => t.Activities)
                .Where(t => t.AssigneeUserId == user.Id &&
                            t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Cancelled)
                .OrderBy(t => t.SlaDueAt)
                .ToListAsync();

            TotalAssignedToMe = AssignedToMe.Count;

            CustomerRepliedCount = AssignedToMe.Count(t =>
            {
                var lastComment = t.Comments.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
                return lastComment?.UserRole == "Customer";
            });

            var userProfile = await context.UserProfiles
                .Include(up => up.Team)
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            if (userProfile?.Team != null)
            {
                TeamName = userProfile.Team.Name;

                MyTeamTickets = await context.Tickets
                    .Include(t => t.Customer)
                    .Include(t => t.Comments)
                    .Include(t => t.Activities)
                    .Where(t => t.AssignedGroup == userProfile.Team.Group &&
                                t.Status != TicketStatus.Closed &&
                                t.Status != TicketStatus.Resolved &&
                                t.Status != TicketStatus.Cancelled)
                    .OrderBy(t => t.SlaDueAt)
                    .ToListAsync();

                TotalTeamTickets = MyTeamTickets.Count;
            }

            var now = DateTime.Now;
            AtRiskCount = AssignedToMe.Count(t =>
            {
                var state = SlaPolicy.GetState(t, now);
                return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
            });

            BreachedCount = AssignedToMe.Count(t => SlaPolicy.GetState(t, now) == SlaState.Breached);
        }

        public List<Ticket> GetMyAtRiskTickets()
        {
            var now = DateTime.Now;
            // IDE0305: collection expression
            return [.. AssignedToMe.Where(t =>
            {
                var state = SlaPolicy.GetState(t, now);
                return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
            })];
        }

        public List<Ticket> GetMyBreachedTickets()
        {
            var now = DateTime.Now;
            return [.. AssignedToMe.Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached)];
        }

        public List<Ticket> GetMyInProgressTickets()
            => [.. AssignedToMe.Where(t => t.Status == TicketStatus.InProgress)];

        public List<Ticket> GetTeamAtRiskTickets()
        {
            var now = DateTime.Now;
            return [.. MyTeamTickets.Where(t =>
            {
                var state = SlaPolicy.GetState(t, now);
                return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
            })];
        }

        public List<Ticket> GetTeamBreachedTickets()
        {
            var now = DateTime.Now;
            return [.. MyTeamTickets.Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached)];
        }

        public List<Ticket> GetTeamUnassignedTickets()
            => [.. MyTeamTickets.Where(t => string.IsNullOrEmpty(t.AssigneeUserId))];

        public List<Ticket> GetTeamCustomerRepliedTickets()
            => [.. MyTeamTickets.Where(t =>
            {
                var last = t.Comments.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
                return last?.UserRole == "Customer";
            })];
    }
}
