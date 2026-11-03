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
    public class EditModel(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context, SignInManager<IdentityUser> signInManager) : PageModel
    {
        [BindProperty]
        public UserEditViewModel Input { get; set; } = new();

        public SelectList TeamList { get; set; } = default!;
        public SelectList RoleList { get; set; } = default!;
        public IdentityUser? AppUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            AppUser = await userManager.FindByIdAsync(id);
            if (AppUser == null)
                return NotFound();

            // Customer portal users must be managed from the Customers page only.
            if (await IsPortalOnlyUserAsync(AppUser))
                return RedirectToPage("/Customers/Index");

            var profile      = await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);
            var currentRoles = await userManager.GetRolesAsync(AppUser);

            Input = new UserEditViewModel
            {
                UserId      = id,
                Email       = AppUser.Email ?? "",
                DisplayName = profile?.DisplayName ?? AppUser.Email ?? "",
                Department  = profile?.Department,
                JobTitle    = profile?.JobTitle,
                PhoneNumber = profile?.PhoneNumber ?? AppUser.PhoneNumber,
                TeamId      = profile?.TeamId,
                IsActive    = profile?.IsActive ?? true,
                Role        = currentRoles.FirstOrDefault() ?? "Agent"
            };

            await LoadDropdownsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.FindByIdAsync(Input.UserId);
            if (user == null)
                return NotFound();

            // Re-check on POST to prevent crafted requests bypassing the GET guard.
            if (await IsPortalOnlyUserAsync(user))
                return Forbid();

            var currentUser   = await userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";

            // Server-side role validation - the dropdown excludes Customer but a
            // crafted POST could still submit it. Reject any disallowed role value.
            var allowedRoles = await roleManager.Roles
                .Where(r => r.Name != null && r.Name != "Customer")
                .Select(r => r.Name!)
                .ToListAsync();

            if (!string.IsNullOrEmpty(Input.Role) && !allowedRoles.Contains(Input.Role))
                ModelState.AddModelError("Input.Role", "Selected role is not valid for internal staff.");

            if (Input.TeamId.HasValue && !await context.Teams.AnyAsync(t => t.Id == Input.TeamId && t.IsActive))
                ModelState.AddModelError("Input.TeamId", "Select an active team.");

            // Prevent a manager from deactivating their own account.
            if (currentUserId == user.Id && !Input.IsActive)
                ModelState.AddModelError("Input.IsActive", "You cannot deactivate your own account.");

            // Prevent a manager from removing their own Manager role.
            if (currentUserId == user.Id &&
                await userManager.IsInRoleAsync(user, "Manager") &&
                Input.Role != "Manager")
            {
                ModelState.AddModelError("Input.Role", "You cannot remove the Manager role from your own account.");
            }

            if (!ModelState.IsValid)
            {
                // Reload AppUser so the view can render the email heading correctly.
                AppUser = user;
                await LoadDropdownsAsync();
                return Page();
            }

            user.PhoneNumber = Input.PhoneNumber;
            await userManager.UpdateAsync(user);

            if (!Input.IsActive)
            {
                await userManager.SetLockoutEnabledAsync(user, true);
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
            else
            {
                await userManager.SetLockoutEndDateAsync(user, null);
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(Input.Role))
                await userManager.AddToRoleAsync(user, Input.Role);

            var profile = await context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == Input.UserId);

            if (profile == null)
            {
                context.UserProfiles.Add(new UserProfile
                {
                    UserId      = Input.UserId,
                    DisplayName = Input.DisplayName,
                    Department  = Input.Department,
                    JobTitle    = Input.JobTitle,
                    PhoneNumber = Input.PhoneNumber,
                    TeamId      = Input.TeamId,
                    IsActive    = Input.IsActive,
                    CreatedBy   = currentUserId
                });
            }
            else
            {
                profile.DisplayName = Input.DisplayName;
                profile.Department  = Input.Department;
                profile.JobTitle    = Input.JobTitle;
                profile.PhoneNumber = Input.PhoneNumber;
                profile.TeamId      = Input.TeamId;
                profile.IsActive    = Input.IsActive;
                profile.ModifiedBy  = currentUserId;
                profile.UpdatedAt   = DateTime.Now;
            }

            await context.SaveChangesAsync();

            await userManager.UpdateSecurityStampAsync(user);
            if (currentUserId == user.Id) await signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Returns true for an actual portal link or Customer role.
        /// Such users are customer portal accounts and must not be edited here.
        /// </summary>
        private async Task<bool> IsPortalOnlyUserAsync(IdentityUser user)
        {
            return await context.CustomerUsers.IgnoreQueryFilters().AnyAsync(c => c.UserId == user.Id)
                || await userManager.IsInRoleAsync(user, "Customer");
        }

        private async Task LoadDropdownsAsync()
        {
            var teams = await context.Teams
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            TeamList = new SelectList(teams, "Id", "Name");

            // Exclude the Customer role - it is not a valid role for internal staff.
            var roles = await roleManager.Roles
                .Where(r => r.Name != "Customer")
                .OrderBy(r => r.Name)
                .ToListAsync();

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