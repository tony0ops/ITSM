using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Users
{
    [Authorize(Roles = "Manager")]
    public class EditModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public EditModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [BindProperty]
        public UserEditViewModel Input { get; set; } = new();

        public SelectList TeamList { get; set; } = default!;
        public SelectList RoleList { get; set; } = default!;
        public IdentityUser? AppUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            AppUser = await _userManager.FindByIdAsync(id);
            if (AppUser == null)
                return NotFound();

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == id);

            var currentRoles = await _userManager.GetRolesAsync(AppUser);

            Input = new UserEditViewModel
            {
                UserId = id,
                Email = AppUser.Email ?? "",
                DisplayName = profile?.DisplayName ?? AppUser.Email ?? "",
                Department = profile?.Department,
                JobTitle = profile?.JobTitle,
                PhoneNumber = profile?.PhoneNumber ?? AppUser.PhoneNumber,
                TeamId = profile?.TeamId,
                IsActive = profile?.IsActive ?? true,
                Role = currentRoles.FirstOrDefault() ?? ""
            };

            await LoadDropdownsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
                return NotFound();

            // Update user phone
            user.PhoneNumber = Input.PhoneNumber;
            await _userManager.UpdateAsync(user);

            // Sync IsActive with Identity lockout - prevents inactive users from logging in
            if (!Input.IsActive)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }

            // Update role assignment
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            if (!string.IsNullOrEmpty(Input.Role))
            {
                await _userManager.AddToRoleAsync(user, Input.Role);
            }

            // Update or create profile
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == Input.UserId);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = Input.UserId,
                    DisplayName = Input.DisplayName,
                    Department = Input.Department,
                    JobTitle = Input.JobTitle,
                    PhoneNumber = Input.PhoneNumber,
                    TeamId = Input.TeamId,
                    IsActive = Input.IsActive,
                    CreatedBy = User.Identity?.Name ?? "system"
                };
                _context.UserProfiles.Add(profile);
            }
            else
            {
                profile.DisplayName = Input.DisplayName;
                profile.Department = Input.Department;
                profile.JobTitle = Input.JobTitle;
                profile.PhoneNumber = Input.PhoneNumber;
                profile.TeamId = Input.TeamId;
                profile.IsActive = Input.IsActive;
                profile.ModifiedBy = User.Identity?.Name ?? "system";
                profile.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToPage("./Index");
        }

        private async Task LoadDropdownsAsync()
        {
            var teams = await _context.Teams
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            TeamList = new SelectList(teams, "Id", "Name");

            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            RoleList = new SelectList(roles, "Name", "Name");
        }

        public class UserEditViewModel
        {
            public string UserId { get; set; } = "";
            public string Email { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string? Department { get; set; }
            public string? JobTitle { get; set; }
            public string? PhoneNumber { get; set; }
            public int? TeamId { get; set; }
            public bool IsActive { get; set; } = true;
            public string? Role { get; set; }
        }
    }
}