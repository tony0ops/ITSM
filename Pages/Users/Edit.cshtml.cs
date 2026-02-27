using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Users
{
    public class EditModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public EditModel(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public UserEditViewModel Input { get; set; } = new();

        public SelectList TeamList { get; set; } = default!;
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

            Input = new UserEditViewModel
            {
                UserId = id,
                Email = AppUser.Email ?? "",
                DisplayName = profile?.DisplayName ?? AppUser.Email ?? "",
                Department = profile?.Department,
                JobTitle = profile?.JobTitle,
                PhoneNumber = profile?.PhoneNumber ?? AppUser.PhoneNumber,
                TeamId = profile?.TeamId,
                IsActive = profile?.IsActive ?? true
            };

            await LoadTeamsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTeamsAsync();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
                return NotFound();

            // Update user phone
            user.PhoneNumber = Input.PhoneNumber;
            await _userManager.UpdateAsync(user);

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

        private async Task LoadTeamsAsync()
        {
            var teams = await _context.Teams
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            TeamList = new SelectList(teams, "Id", "Name");
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
        }
    }
}