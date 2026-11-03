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
        public int CustomerRepliedCount { get; set; }
        public bool HasTeamAssignment { get; set; }
        public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 25);
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

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
                HasTeamAssignment = false;
                return Page();
            }

            HasTeamAssignment = true;
            var userGroup = profile.Team.Group;
            GroupName = profile.Team.Group.ToString().Replace("_", " ");
            TeamName = profile.Team.Name;

            var queueQuery = _context.Tickets.Where(t => t.AssignedGroup == userGroup &&
                            t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Cancelled &&
                            t.Status != TicketStatus.Resolved);
            var allTickets = await queueQuery.AsNoTracking().ToListAsync();

            var now = DateTime.Now;
            AtRiskCount = allTickets.Count(t =>
            {
                var state = SlaPolicy.GetState(t, now);
                return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
            });

            BreachedCount = allTickets.Count(t => SlaPolicy.GetState(t, now) == SlaState.Breached);
            InProgressCount = allTickets.Count(t => t.Status == TicketStatus.InProgress);
            CustomerRepliedCount = await CountCustomerRepliedAsync(queueQuery);

            Pagination = PaginationViewModel.Create(allTickets.Count, PageNumber, PageSize,
                routeValues: PaginationViewModel.ReadRouteValues(Request.Query));
            var ids = allTickets.OrderBy(t => SlaPolicy.GetEffectiveDueAt(t, now))
                .Skip((Pagination.PageNumber - 1) * Pagination.PageSize).Take(Pagination.PageSize)
                .Select(t => t.Id).ToList();
            if (ids.Count > 0)
            {
                var pageRows = await _context.Tickets.AsNoTracking().AsSplitQuery()
                    .Include(t => t.Customer).Include(t => t.Comments).Include(t => t.Activities)
                    .Where(t => ids.Contains(t.Id)).ToListAsync();
                var byId = pageRows.ToDictionary(t => t.Id);
                Tickets = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
            }

            return Page();
        }

        private async Task<int> CountCustomerRepliedAsync(IQueryable<Ticket> ticketQuery)
        {
            var ids = ticketQuery.Select(t => t.Id);
            var customers = await _context.TicketComments.AsNoTracking()
                .Where(c => ids.Contains(c.TicketId) && c.UserRole == "Customer")
                .GroupBy(c => c.TicketId).Select(g => new { Id = g.Key, At = g.Max(c => c.CreatedAt) })
                .ToDictionaryAsync(x => x.Id, x => x.At);
            var staff = await _context.TicketComments.AsNoTracking()
                .Where(c => ids.Contains(c.TicketId) && c.UserRole != "Customer" && c.IsCustomerVisible && !c.IsInternal)
                .GroupBy(c => c.TicketId).Select(g => new { Id = g.Key, At = g.Max(c => c.CreatedAt) })
                .ToDictionaryAsync(x => x.Id, x => x.At);
            return customers.Count(item => !staff.TryGetValue(item.Key, out var staffAt) || item.Value > staffAt);
        }

        public List<Ticket> GetAtRiskTickets()
        {
            var now = DateTime.Now;
            return Tickets.Where(t =>
            {
                var state = SlaPolicy.GetState(t, now);
                return state == SlaState.Yellow || state == SlaState.Red || state == SlaState.Breached;
            }).ToList();
        }

        public List<Ticket> GetBreachedTickets()
        {
            var now = DateTime.Now;
            return Tickets.Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached).ToList();
        }

        public List<Ticket> GetInProgressTickets()
            => Tickets.Where(t => t.Status == TicketStatus.InProgress).ToList();

        public List<Ticket> GetCustomerRepliedTickets()
            => Tickets.Where(TicketHelper.IsCustomerReplied).ToList();
    }
}
