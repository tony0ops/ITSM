using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    public class IndexModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager) : PageModel
    {
        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public TicketStatus? Status { get; set; }
        [BindProperty(SupportsGet = true)] public Severity? Severity { get; set; }
        [BindProperty(SupportsGet = true)] public TicketCategory? Category { get; set; }
        [BindProperty(SupportsGet = true)] public AssignmentGroup? Group { get; set; }
        [BindProperty(SupportsGet = true)] public string? Sla { get; set; }
        [BindProperty(SupportsGet = true)] public string? SortBy { get; set; } = "created";
        [BindProperty(SupportsGet = true)] public string? SortOrder { get; set; } = "desc";
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

        public IList<Ticket> Tickets { get; set; } = [];
        public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 25);
        public Dictionary<string, string> UserEmails { get; set; } = [];
        public Dictionary<int, TicketActivityInfo> ActivityMap { get; set; } = [];

        public class TicketActivityInfo
        {
            public DateTime LastActivityAt { get; init; }
            public bool CustomerReplied { get; init; }
        }

        public string ToggleOrder(string column)
        {
            if (string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase))
                return string.Equals(SortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
            return "asc";
        }

        public string SortIndicator(string column)
        {
            if (!string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase))
                return "";
            return string.Equals(SortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? " ▲" : " ▼";
        }

        public async Task OnGetAsync()
        {
            var query = context.Tickets.AsNoTracking().AsQueryable();

            if (!Status.HasValue)
            {
                query = query.Where(t =>
                    t.Status != TicketStatus.Closed &&
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Cancelled);
            }

            if (!string.IsNullOrWhiteSpace(Q))
            {
                query = query.Where(t =>
                    t.Title.Contains(Q) ||
                    t.Description.Contains(Q) ||
                    (t.CustomerName != null && t.CustomerName.Contains(Q)) ||
                    (t.Customer != null && t.Customer.CompanyName.Contains(Q)) ||
                    (t.Number != null && t.Number.Contains(Q)));
            }

            if (Status.HasValue)   query = query.Where(t => t.Status == Status.Value);
            if (Severity.HasValue) query = query.Where(t => t.Severity == Severity.Value);
            if (Category.HasValue) query = query.Where(t => t.Category == Category.Value);
            if (Group.HasValue)    query = query.Where(t => t.AssignedGroup == Group.Value);

            var sort = SortBy?.ToLowerInvariant() ?? "created";
            var asc  = string.Equals(SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            IQueryable<Ticket> orderedQuery = query;

            if (sort != "last_activity")
            {
                orderedQuery = (sort, asc) switch
                {
                    ("number",   true)  => query.OrderBy(t => t.Number),
                    ("number",   false) => query.OrderByDescending(t => t.Number),
                    ("title",    true)  => query.OrderBy(t => t.Title),
                    ("title",    false) => query.OrderByDescending(t => t.Title),
                    ("category", true)  => query.OrderBy(t => t.Category),
                    ("category", false) => query.OrderByDescending(t => t.Category),
                    ("severity", true)  => query.OrderBy(t => t.Severity),
                    ("severity", false) => query.OrderByDescending(t => t.Severity),
                    ("status",   true)  => query.OrderBy(t => t.Status),
                    ("status",   false) => query.OrderByDescending(t => t.Status),
                    ("group",    true)  => query.OrderBy(t => t.AssignedGroup),
                    ("group",    false) => query.OrderByDescending(t => t.AssignedGroup),
                    ("assigned", true)  => query.OrderBy(t => t.AssigneeUserId),
                    ("assigned", false) => query.OrderByDescending(t => t.AssigneeUserId),
                    ("customer", true)  => query.OrderBy(t => t.Customer != null ? t.Customer.CompanyName : ""),
                    ("customer", false) => query.OrderByDescending(t => t.Customer != null ? t.Customer.CompanyName : ""),
                    ("sla",      true)  => query.OrderBy(t => t.SlaDueAt),
                    ("sla",      false) => query.OrderByDescending(t => t.SlaDueAt),
                    ("created",  true)  => query.OrderBy(t => t.CreatedAt),
                    ("created",  false) => query.OrderByDescending(t => t.CreatedAt),
                    _                   => query.OrderByDescending(t => t.CreatedAt)
                };
            }

            var requiresCalculatedPaging = sort is "last_activity" or "sla" || !string.IsNullOrWhiteSpace(Sla);
            var routeValues = PaginationViewModel.ReadRouteValues(Request.Query);
            if (!requiresCalculatedPaging)
            {
                var count = await orderedQuery.CountAsync();
                Pagination = PaginationViewModel.Create(count, PageNumber, PageSize, routeValues: routeValues);
                var pageIds = await orderedQuery.Skip((Pagination.PageNumber - 1) * Pagination.PageSize)
                    .Take(Pagination.PageSize).Select(t => t.Id).ToListAsync();
                Tickets = await LoadTicketPageAsync(pageIds);
            }
            else
            {
                // Calculated SLA/last-activity filters scan scalar ticket rows; activity
                // timestamps are aggregated in SQL and full ticket relations load only for the page.
                var list = await orderedQuery.ToListAsync();
                var now = DateTime.Now;

                if (sort == "last_activity")
                {
                    var candidateIds = orderedQuery.Select(t => t.Id);
                    var latestCommentActivity = await context.TicketComments.AsNoTracking()
                        .Where(c => candidateIds.Contains(c.TicketId)).GroupBy(c => c.TicketId)
                        .Select(g => new { Id = g.Key, At = g.Max(c => c.UpdatedAt.HasValue && c.UpdatedAt.Value > c.CreatedAt ? c.UpdatedAt.Value : c.CreatedAt) })
                        .ToDictionaryAsync(x => x.Id, x => x.At);
                    var latestTicketActivity = await context.TicketActivities.AsNoTracking()
                        .Where(a => candidateIds.Contains(a.TicketId)).GroupBy(a => a.TicketId)
                        .Select(g => new { Id = g.Key, At = g.Max(a => a.UpdatedAt.HasValue && a.UpdatedAt.Value > a.CreatedAt ? a.UpdatedAt.Value : a.CreatedAt) })
                        .ToDictionaryAsync(x => x.Id, x => x.At);
                    var latestCustomerReply = await context.TicketComments.AsNoTracking()
                        .Where(c => candidateIds.Contains(c.TicketId) && c.UserRole == "Customer")
                        .GroupBy(c => c.TicketId).Select(g => new { Id = g.Key, At = g.Max(c => c.CreatedAt) })
                        .ToDictionaryAsync(x => x.Id, x => x.At);
                    var latestVisibleStaffReply = await context.TicketComments.AsNoTracking()
                        .Where(c => candidateIds.Contains(c.TicketId) && c.UserRole != "Customer" && c.IsCustomerVisible && !c.IsInternal)
                        .GroupBy(c => c.TicketId).Select(g => new { Id = g.Key, At = g.Max(c => c.CreatedAt) })
                        .ToDictionaryAsync(x => x.Id, x => x.At);

                    foreach (var t in list)
                    {
                        var lastActivityAt = t.UpdatedAt.HasValue && t.UpdatedAt.Value > t.CreatedAt
                            ? t.UpdatedAt.Value : t.CreatedAt;
                        if (latestCommentActivity.TryGetValue(t.Id, out var commentAt) && commentAt > lastActivityAt)
                            lastActivityAt = commentAt;
                        if (latestTicketActivity.TryGetValue(t.Id, out var activityAt) && activityAt > lastActivityAt)
                            lastActivityAt = activityAt;
                        var customerReplied = latestCustomerReply.TryGetValue(t.Id, out var customerAt) &&
                            (!latestVisibleStaffReply.TryGetValue(t.Id, out var staffAt) || customerAt > staffAt);
                        ActivityMap[t.Id] = new TicketActivityInfo
                        {
                            LastActivityAt = lastActivityAt,
                            CustomerReplied = customerReplied
                        };
                    }
                    list = asc ? list.OrderBy(t => ActivityMap[t.Id].LastActivityAt).ToList()
                        : list.OrderByDescending(t => ActivityMap[t.Id].LastActivityAt).ToList();
                }

                if (sort == "sla")
                    list = asc ? list.OrderBy(t => SlaPolicy.GetEffectiveDueAt(t, now)).ToList()
                        : list.OrderByDescending(t => SlaPolicy.GetEffectiveDueAt(t, now)).ToList();
                if (!string.IsNullOrWhiteSpace(Sla))
                {
                    var slaKey = Sla.Trim().ToLowerInvariant();
                    list = slaKey switch
                    {
                        "paused"   => list.Where(t => SlaPolicy.IsSlaPaused(t.Status)).ToList(),
                        "green"    => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Green).ToList(),
                        "yellow"   => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Yellow).ToList(),
                        "red"      => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Red).ToList(),
                        "breached" => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached).ToList(),
                        "atrisk"   => list.Where(t => SlaPolicy.GetState(t, now) is SlaState.Yellow or SlaState.Red or SlaState.Breached).ToList(),
                        _ => list
                    };
                }

                Pagination = PaginationViewModel.Create(list.Count, PageNumber, PageSize, routeValues: routeValues);
                var pageIds = list.Skip((Pagination.PageNumber - 1) * Pagination.PageSize)
                    .Take(Pagination.PageSize).Select(t => t.Id).ToList();
                Tickets = await LoadTicketPageAsync(pageIds);
            }

            // Compute helper values from the loaded collections on this page.
            foreach (var t in Tickets)
                ActivityMap[t.Id] = new TicketActivityInfo
                {
                    LastActivityAt = TicketHelper.GetLastActivityAt(t),
                    CustomerReplied = TicketHelper.IsCustomerReplied(t)
                };

            var userIds = Tickets
                .Where(t => !string.IsNullOrEmpty(t.AssigneeUserId))
                .Select(t => t.AssigneeUserId!)
                .Distinct()
                .ToList();

            if (userIds.Count > 0)
            {
                var users = await userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in users)
                    UserEmails[user.Id] = user.Email ?? user.UserName ?? "Unknown";
            }
        }

        private async Task<List<Ticket>> LoadTicketPageAsync(List<int> ids)
        {
            if (ids.Count == 0) return [];
            var tickets = await context.Tickets.AsNoTracking().AsSplitQuery()
                .Include(t => t.Customer)
                .Include(t => t.Comments)
                .Include(t => t.Activities)
                .Where(t => ids.Contains(t.Id)).ToListAsync();
            var byId = tickets.ToDictionary(t => t.Id);
            return ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        }
    }
}
