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
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Team Team { get; set; } = default!;
        public int ActiveMemberCount { get; set; }
        public int OpenTicketCount { get; set; }
        public bool CanDelete => ActiveMemberCount == 0;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return NotFound();

            Team = team;
            ActiveMemberCount = team.Members.Count(m => m.IsActive);
            OpenTicketCount = await _context.Tickets
                .CountAsync(t => t.AssignedGroup == team.Group &&
                                 t.Status != TicketStatus.Closed &&
                                 t.Status != TicketStatus.Resolved &&
                                 t.Status != TicketStatus.Cancelled);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return NotFound();

            // Block deletion if team has active members
            var activeMembers = team.Members.Count(m => m.IsActive);
            if (activeMembers > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete team '{team.Name}' — it still has {activeMembers} active member(s). Remove them first.";
                return RedirectToPage("./Details", new { id });
            }

            // Unlink any inactive members still referencing this team
            foreach (var member in team.Members)
            {
                member.TeamId = null;
                member.UpdatedAt = DateTime.Now;
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Team '{team.Name}' has been permanently deleted.";
            return RedirectToPage("./Index");
        }
    }
}