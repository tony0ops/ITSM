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

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            // Load only internal staff: users who have a UserProfile.
            // Customer portal users are managed from the Customers page only.
            var profiles = await context.UserProfiles
                .Include(up => up.Team)
                .Include(up => up.User)
                .AsNoTracking()
                .ToListAsync();

            var viewModels = new List<UserViewModel>();
            foreach (var profile in profiles)
            {
                var u = profile.User;
                if (u == null) continue;

                var roles = await userManager.GetRolesAsync(u);

                viewModels.Add(new UserViewModel
                {
                    UserId      = u.Id,
                    Email       = u.Email ?? "N/A",
                    UserName    = u.UserName ?? "N/A",
                    DisplayName = profile.DisplayName,
                    Department  = profile.Department,
                    JobTitle    = profile.JobTitle,
                    TeamId      = profile.TeamId,
                    TeamName    = profile.Team?.Name,
                    IsActive    = profile.IsActive,
                    HasProfile  = true,
                    Role        = roles.FirstOrDefault() ?? "None"
                });
            }

            Users = [.. viewModels.OrderBy(u => u.Email)];

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                Users = [.. Users.Where(u =>
                    (u.Email?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.DisplayName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.TeamName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Role?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false))];
            }
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
            public bool HasProfile { get; set; }
            public string Role { get; set; } = "None";
        }
    }
}