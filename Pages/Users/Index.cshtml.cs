using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Users
{
    [Authorize(Roles = "Manager")]
    public class IndexModel(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context) : PageModel
    {
        public List<UserViewModel> Users { get; set; } = [];
        public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 25);

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            var query = userManager.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToUpper();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToUpper().Contains(term)) ||
                    (u.UserName != null && u.UserName.ToUpper().Contains(term)) ||
                    context.UserProfiles.Any(p => p.UserId == u.Id &&
                        ((p.DisplayName != null && p.DisplayName.ToUpper().Contains(term)) ||
                         (p.Team != null && p.Team.Name.ToUpper().Contains(term)))) ||
                    context.UserRoles.Any(ur => ur.UserId == u.Id && context.Roles.Any(r =>
                        r.Id == ur.RoleId && r.Name != null && r.Name.ToUpper().Contains(term))) ||
                    context.CustomerUsers.IgnoreQueryFilters().Any(cu => cu.UserId == u.Id &&
                        cu.Customer.CompanyName.ToUpper().Contains(term)));
            }

            var count = await query.CountAsync();
            Pagination = PaginationViewModel.Create(count, PageNumber, PageSize,
                routeValues: PaginationViewModel.ReadRouteValues(Request.Query));
            var accountPage = await query.OrderBy(u => u.Email)
                .Skip((Pagination.PageNumber - 1) * Pagination.PageSize).Take(Pagination.PageSize)
                .ToListAsync();
            var userIds = accountPage.Select(u => u.Id).ToArray();
            var profiles = await context.UserProfiles.Include(p => p.Team).AsNoTracking()
                .Where(p => userIds.Contains(p.UserId)).ToDictionaryAsync(p => p.UserId);
            var links = await context.CustomerUsers.IgnoreQueryFilters().Include(c => c.Customer)
                .AsNoTracking().Where(c => userIds.Contains(c.UserId)).ToDictionaryAsync(c => c.UserId);
            var viewModels = new List<UserViewModel>();
            foreach (var u in accountPage)
            {
                profiles.TryGetValue(u.Id, out var profile);
                links.TryGetValue(u.Id, out var link);
                var roles = await userManager.GetRolesAsync(u);
                viewModels.Add(new UserViewModel
                {
                    UserId = u.Id, Email = u.Email ?? "N/A", UserName = u.UserName ?? "N/A",
                    DisplayName = profile?.DisplayName ?? u.Email ?? "", Department = profile?.Department,
                    JobTitle = profile?.JobTitle, TeamId = profile?.TeamId, TeamName = profile?.Team?.Name,
                    IsActive = (profile?.IsActive ?? true) && (link == null || (link.IsActive && link.Customer.IsActive && !link.Customer.IsDeleted)),
                    HasProfile = profile != null, Role = string.Join(", ", roles.DefaultIfEmpty("Unassigned")),
                    IsPortal = link != null || roles.Contains("Customer"),
                    CustomerId = link != null && !link.Customer.IsDeleted ? link.CustomerId : null
                });
            }

            Users = [.. viewModels.OrderBy(u => u.Email)];
        }

        public class UserViewModel
        {
            public string UserId { get; set; } = "";
            public string Email { get; set; } = "";
            public string UserName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string? Department { get; set; }
            public string? JobTitle { get; set; }
            public int? TeamId { get; set; }
            public string? TeamName { get; set; }
            public bool IsActive { get; set; }
            public bool IsPortal { get; set; }
            public int? CustomerId { get; set; }
            public bool HasProfile { get; set; }
            public string Role { get; set; } = "None";
        }
    }
}
