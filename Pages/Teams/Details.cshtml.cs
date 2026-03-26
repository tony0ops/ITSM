using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Teams
{
    [Authorize(Roles = "Manager")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Team Team { get; set; } = default!;
        public List<UserProfile> Members { get; set; } = new();
        public List<UserProfile> AvailableUsers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null)
                return NotFound();

            Team = team;
            Members = team.Members.Where(m => m.IsActive).OrderBy(m => m.DisplayName).ToList();

            // Only show users with no team — never silently steal from another team
            AvailableUsers = await _context.UserProfiles
                .Include(up => up.User)
                .Where(up => up.IsActive && up.TeamId == null)
                .OrderBy(up => up.DisplayName)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddMemberAsync(int teamId, int userId)
        {
            var profile = await _context.UserProfiles.FindAsync(userId);
            if (profile == null)
                return NotFound();

            // Safety check: only assign if user has no team
            if (profile.TeamId != null)
            {
                TempData["ErrorMessage"] = "User is already assigned to another team. Remove them first.";
                return RedirectToPage(new { id = teamId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            profile.TeamId = teamId;
            profile.UpdatedAt = DateTime.Now;
            profile.ModifiedBy = currentUser?.Id ?? "";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Member added to team.";

            return RedirectToPage(new { id = teamId });
        }

        public async Task<IActionResult> OnPostRemoveMemberAsync(int teamId, int userId)
        {
            var profile = await _context.UserProfiles.FindAsync(userId);
            if (profile != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                profile.TeamId = null;
                profile.UpdatedAt = DateTime.Now;
                profile.ModifiedBy = currentUser?.Id ?? "";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Member removed from team.";
            }

            return RedirectToPage(new { id = teamId });
        }
    }
}