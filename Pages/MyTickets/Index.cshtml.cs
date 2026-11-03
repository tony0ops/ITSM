using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        public int TeamCustomerRepliedCount { get; set; }
        public PaginationViewModel AssignedPagination { get; set; } = PaginationViewModel.Create(0, 1, 25, "MyPageNumber");
        public PaginationViewModel TeamPagination { get; set; } = PaginationViewModel.Create(0, 1, 25, "TeamPageNumber");
        [BindProperty(SupportsGet = true)] public int MyPageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int TeamPageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

        public async Task OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return;

            var assignedQuery = context.Tickets.Where(t => t.AssigneeUserId == user.Id &&
                t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Cancelled);
            var assignedCandidates = await assignedQuery.AsNoTracking().ToListAsync();
            TotalAssignedToMe = assignedCandidates.Count;
            CustomerRepliedCount = await CountCustomerRepliedAsync(assignedQuery);
            var now = DateTime.Now;
            AtRiskCount = assignedCandidates.Count(t => SlaPolicy.GetState(t, now) is SlaState.Yellow or SlaState.Red or SlaState.Breached);
            BreachedCount = assignedCandidates.Count(t => SlaPolicy.GetState(t, now) == SlaState.Breached);
            var routeValues = PaginationViewModel.ReadRouteValues(Request.Query, "MyPageNumber");
            AssignedPagination = PaginationViewModel.Create(TotalAssignedToMe, MyPageNumber, PageSize,
                "MyPageNumber", routeValues);
            var assignedIds = assignedCandidates.OrderBy(t => SlaPolicy.GetEffectiveDueAt(t, now))
                .Skip((AssignedPagination.PageNumber - 1) * AssignedPagination.PageSize)
                .Take(AssignedPagination.PageSize).Select(t => t.Id).ToList();
            AssignedToMe = await LoadTicketPageAsync(assignedIds);

            var userProfile = await context.UserProfiles
                .Include(up => up.Team)
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            if (userProfile?.Team != null)
            {
                TeamName = userProfile.Team.Name;

                var teamQuery = context.Tickets.Where(t => t.AssignedGroup == userProfile.Team.Group &&
                    t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Cancelled);
                var teamCandidates = await teamQuery.AsNoTracking().ToListAsync();
                TotalTeamTickets = teamCandidates.Count;
                TeamCustomerRepliedCount = await CountCustomerRepliedAsync(teamQuery);
                var teamRouteValues = PaginationViewModel.ReadRouteValues(Request.Query, "TeamPageNumber");
                TeamPagination = PaginationViewModel.Create(TotalTeamTickets, TeamPageNumber, PageSize,
                    "TeamPageNumber", teamRouteValues);
                var teamIds = teamCandidates.OrderBy(t => SlaPolicy.GetEffectiveDueAt(t, now))
                    .Skip((TeamPagination.PageNumber - 1) * TeamPagination.PageSize)
                    .Take(TeamPagination.PageSize).Select(t => t.Id).ToList();
                MyTeamTickets = await LoadTicketPageAsync(teamIds);
            }
        }

        private async Task<int> CountCustomerRepliedAsync(IQueryable<Ticket> ticketQuery)
        {
            var ids = ticketQuery.Select(t => t.Id);
            var lastCustomer = await context.TicketComments.AsNoTracking()
                .Where(c => ids.Contains(c.TicketId) && c.UserRole == "Customer")
                .GroupBy(c => c.TicketId).Select(g => new { Id = g.Key, At = g.Max(c => c.CreatedAt) })
                .ToDictionaryAsync(x => x.Id, x => x.At);
            var lastStaff = await context.TicketComments.AsNoTracking()
                .Where(c => ids.Contains(c.TicketId) && c.UserRole != "Customer" && c.IsCustomerVisible && !c.IsInternal)
                .GroupBy(c => c.TicketId).Select(g => new { Id = g.Key, At = g.Max(c => c.CreatedAt) })
                .ToDictionaryAsync(x => x.Id, x => x.At);
            return lastCustomer.Count(item => !lastStaff.TryGetValue(item.Key, out var staffAt) || item.Value > staffAt);
        }

        private async Task<List<Ticket>> LoadTicketPageAsync(List<int> ids)
        {
            if (ids.Count == 0) return [];
            var rows = await context.Tickets.AsNoTracking().AsSplitQuery()
                .Include(t => t.Customer).Include(t => t.Comments).Include(t => t.Activities)
                .Where(t => ids.Contains(t.Id)).ToListAsync();
            var byId = rows.ToDictionary(t => t.Id);
            return ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
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
            => [.. MyTeamTickets.Where(TicketHelper.IsCustomerReplied)];
    }
}
