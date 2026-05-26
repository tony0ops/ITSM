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

        public IList<Ticket> Tickets { get; set; } = [];
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
            var query = context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Comments)
                .Include(t => t.Activities)  // required for TicketHelper.GetLastActivityAt
                .AsQueryable();

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

            if (sort != "last_activity")
            {
                query = (sort, asc) switch
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
                    _                   => query.OrderByDescending(t => t.CreatedAt)
                };
            }

            var list = await query.ToListAsync();

            // Use centralized helper - consistent with _TicketTable, MyTickets, and NewQueue.
            foreach (var t in list)
            {
                ActivityMap[t.Id] = new TicketActivityInfo
                {
                    LastActivityAt  = TicketHelper.GetLastActivityAt(t),
                    CustomerReplied = TicketHelper.IsCustomerReplied(t)
                };
            }

            if (sort == "last_activity")
            {
                list = asc
                    ? list.OrderBy(t => ActivityMap[t.Id].LastActivityAt).ToList()
                    : list.OrderByDescending(t => ActivityMap[t.Id].LastActivityAt).ToList();
            }

            var now = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(Sla))
            {
                var slaKey = Sla.Trim().ToLowerInvariant();
                list = slaKey switch
                {
                    "green"    => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Green).ToList(),
                    "yellow"   => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Yellow).ToList(),
                    "red"      => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Red).ToList(),
                    "breached" => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached).ToList(),
                    "atrisk"   => list.Where(t =>
                    {
                        var st = SlaPolicy.GetState(t, now);
                        return st == SlaState.Yellow || st == SlaState.Red || st == SlaState.Breached;
                    }).ToList(),
                    _ => list
                };
            }

            Tickets = list;

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
    }
}
