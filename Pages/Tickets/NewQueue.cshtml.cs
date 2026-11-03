using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    // IDE0290: primary constructor
    public class NewQueueModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
    {
        public List<Ticket> Tickets { get; set; } = [];
        public bool CanClaim { get; set; }
        public bool HasUnroutedTeam { get; set; }
        public AssignmentGroup? ClaimGroup { get; set; }
        public string? ClaimTeamName { get; set; }
        [BindProperty] public List<int> SelectedTicketIds { get; set; } = [];
        public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 25);
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

        public async Task OnGetAsync()
        {
            await LoadClaimAccessAsync();
            var query = context.Tickets
                .AsSplitQuery()
                .Include(t => t.Customer)
                .Include(t => t.Comments)
                .Include(t => t.Activities)
                 .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Cancelled && string.IsNullOrEmpty(t.AssigneeUserId))
                .OrderBy(t => t.CreatedAt);
            var page = await PagedResult<Ticket>.CreateAsync(query, PageNumber, PageSize,
                routeValues: PaginationViewModel.ReadRouteValues(Request.Query));
            Pagination = page.Pagination;
            Tickets = page.Items;
        }

        public async Task<IActionResult> OnPostClaimSelectedAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!User.IsInRole("Agent") && !User.IsInRole("Manager")) return Forbid();

            var profile = await context.UserProfiles.Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);
            if (profile?.Team == null || !profile.Team.IsActive)
            {
                TempData["ErrorMessage"] = "Ask a Manager to assign you to an active team before claiming tickets.";
                return RedirectToPage();
            }
            if (profile.Team.Group == AssignmentGroup.Unassigned)
            {
                TempData["ErrorMessage"] = "Your team needs a real assignment group before you can claim tickets. Ask a Manager to update the team.";
                return RedirectToPage();
            }

            var selectedIds = SelectedTicketIds.Distinct().Take(100).ToArray();
            if (selectedIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Select one or more tickets to claim.";
                return RedirectToPage();
            }

            var userName = user.Email ?? user.UserName ?? "Staff member";
            var now = DateTime.Now;
            var candidates = await context.Tickets.AsNoTracking()
                .Where(t => selectedIds.Contains(t.Id) && string.IsNullOrEmpty(t.AssigneeUserId) &&
                    t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Cancelled &&
                    (t.AssignedGroup == AssignmentGroup.Unassigned || t.AssignedGroup == profile.Team.Group))
                .Select(t => new { t.Id, t.AssignedGroup })
                .ToListAsync();

            var claimed = 0;
            await using var transaction = await context.Database.BeginTransactionAsync();
            foreach (var candidate in candidates)
            {
                var oldGroup = candidate.AssignedGroup;
                var changed = await context.Tickets
                    .Where(t => t.Id == candidate.Id && string.IsNullOrEmpty(t.AssigneeUserId) &&
                        t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved &&
                        t.Status != TicketStatus.Cancelled &&
                        (t.AssignedGroup == AssignmentGroup.Unassigned || t.AssignedGroup == profile.Team.Group))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(t => t.AssigneeUserId, user.Id)
                        .SetProperty(t => t.AssignedGroup, oldGroup == AssignmentGroup.Unassigned ? profile.Team.Group : oldGroup)
                        .SetProperty(t => t.UpdatedAt, now)
                        .SetProperty(t => t.ModifiedBy, user.Id));
                if (changed == 0) continue;

                context.TicketActivities.Add(new TicketActivity
                {
                    TicketId = candidate.Id,
                    Type = ActivityType.Reassigned,
                    CreatedAt = now,
                    UserId = user.Id,
                    UserName = userName,
                    Description = "Ticket claimed",
                    NewValue = userName,
                    IsInternal = false
                });
                if (oldGroup == AssignmentGroup.Unassigned)
                {
                    context.TicketActivities.Add(new TicketActivity
                    {
                        TicketId = candidate.Id,
                        Type = ActivityType.AssignmentGroupChanged,
                        CreatedAt = now,
                        UserId = user.Id,
                        UserName = userName,
                        Description = $"Ticket routed to {profile.Team.Group} when claimed",
                        OldValue = AssignmentGroup.Unassigned.ToString(),
                        NewValue = profile.Team.Group.ToString(),
                        IsInternal = true
                    });
                }
                claimed++;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData[claimed > 0 ? "SuccessMessage" : "ErrorMessage"] = claimed > 0
                ? $"Claimed {claimed} ticket(s)."
                : "None of the selected tickets were still available for your team.";
            return RedirectToPage();
        }

        private async Task LoadClaimAccessAsync()
        {
            if (!User.IsInRole("Agent") && !User.IsInRole("Manager")) return;
            var user = await userManager.GetUserAsync(User);
            if (user == null) return;
            var profile = await context.UserProfiles.Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);
            if (profile?.Team is { IsActive: true } team)
            {
                if (team.Group == AssignmentGroup.Unassigned)
                {
                    HasUnroutedTeam = true;
                    ClaimTeamName = team.Name;
                    return;
                }
                CanClaim = true;
                ClaimGroup = team.Group;
                ClaimTeamName = team.Name;
            }
        }
    }
}
