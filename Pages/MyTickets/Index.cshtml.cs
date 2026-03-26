using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.MyTickets
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Ticket> AssignedToMe { get; set; } = new();
        public List<Ticket> MyTeamTickets { get; set; } = new();
        public string? TeamName { get; set; }
        public int TotalAssignedToMe { get; set; }
        public int TotalTeamTickets { get; set; }
        public int AtRiskCount { get; set; }
        public int BreachedCount { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            AssignedToMe = await _context.Tickets
                .Include(t => t.Customer)
                .Where(t => t.AssigneeUserId == user.Id)
                .Where(t => t.Status != TicketStatus.Closed &&
                           t.Status != TicketStatus.Resolved &&
                           t.Status != TicketStatus.Cancelled)
                .OrderBy(t => t.SlaDueAt)
                .ToListAsync();

            TotalAssignedToMe = AssignedToMe.Count;

            var userProfile = await _context.UserProfiles
                .Include(up => up.Team)
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            if (userProfile?.Team != null)
            {
                TeamName = userProfile.Team.Name;

                // Consistent rule: all open tickets in the team's assignment group
                MyTeamTickets = await _context.Tickets
                    .Include(t => t.Customer)
                    .Where(t => t.AssignedGroup == userProfile.Team.Group)
                    .Where(t => t.Status != TicketStatus.Closed &&
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

        /// <summary>
        /// Get at-risk tickets assigned to me
        /// </summary>
        public List<Ticket> GetMyAtRiskTickets()
        {
            var now = DateTime.Now;
            return AssignedToMe
                .Where(t =>
                {
                    var state = SlaPolicy.GetState(t, now);
                    return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
                })
                .ToList();
        }

        /// <summary>
        /// Get breached tickets assigned to me
        /// </summary>
        public List<Ticket> GetMyBreachedTickets()
        {
            var now = DateTime.Now;
            return AssignedToMe
                .Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached)
                .ToList();
        }

        /// <summary>
        /// Get in-progress tickets assigned to me
        /// </summary>
        public List<Ticket> GetMyInProgressTickets()
        {
            return AssignedToMe
                .Where(t => t.Status == TicketStatus.InProgress)
                .ToList();
        }

        /// <summary>
        /// Get at-risk team tickets
        /// </summary>
        public List<Ticket> GetTeamAtRiskTickets()
        {
            var now = DateTime.Now;
            return MyTeamTickets
                .Where(t =>
                {
                    var state = SlaPolicy.GetState(t, now);
                    return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
                })
                .ToList();
        }

        /// <summary>
        /// Get breached team tickets
        /// </summary>
        public List<Ticket> GetTeamBreachedTickets()
        {
            var now = DateTime.Now;
            return MyTeamTickets
                .Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached)
                .ToList();
        }

        /// <summary>
        /// Get unassigned team tickets
        /// </summary>
        public List<Ticket> GetTeamUnassignedTickets()
        {
            return MyTeamTickets
                .Where(t => string.IsNullOrEmpty(t.AssigneeUserId))
                .ToList();
        }
    }
}
