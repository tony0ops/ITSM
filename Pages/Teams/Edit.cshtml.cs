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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Team Team { get; set; } = default!;

        public int MemberCount { get; set; }
        public int OpenTicketCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null)
                return NotFound();

            Team = team;
            MemberCount = team.Members.Count(m => m.IsActive);
            OpenTicketCount = await _context.Tickets
                .CountAsync(t => t.AssignedGroup == team.Group &&
                                t.Status != TicketStatus.Closed &&
                                t.Status != TicketStatus.Resolved &&
                                t.Status != TicketStatus.Cancelled);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Team.Name = Team.Name?.Trim() ?? "";
            if (Team.Group == AssignmentGroup.Unassigned)
                ModelState.AddModelError("Team.Group", "Choose a support group; Unassigned is reserved for tickets that have not been routed.");
            if (await _context.Teams.AnyAsync(t => t.Id != Team.Id && t.Name.ToLower() == Team.Name.ToLower()))
                ModelState.AddModelError("Team.Name", "A team with this name already exists.");
            if (!ModelState.IsValid)
            {
                await LoadCounts();
                return Page();
            }

            var existing = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == Team.Id);

            if (existing == null)
                return NotFound();

            if (existing.Group != Team.Group && (existing.Members.Any(m => m.IsActive) ||
                await _context.Tickets.AnyAsync(t => t.AssignedGroup == existing.Group &&
                    t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Cancelled)))
            {
                ModelState.AddModelError("Team.Group", "Move active members and open tickets before changing this team's group.");
                await LoadCounts();
                return Page();
            }
            var wasActive = existing.IsActive;
            var isBeingDeactivated = wasActive && !Team.IsActive;

            existing.Name = Team.Name;
            existing.Description = Team.Description;
            existing.Group = Team.Group;
            existing.IsActive = Team.IsActive;
            existing.UpdatedAt = DateTime.Now;

            // When deactivating: unassign all active members
            var unassignedCount = 0;
            if (isBeingDeactivated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var modifiedBy = currentUser?.Id ?? "";

                var activeMembers = existing.Members.Where(m => m.IsActive).ToList();
                unassignedCount = activeMembers.Count;
                foreach (var member in activeMembers)
                {
                    member.TeamId = null;
                    member.UpdatedAt = DateTime.Now;
                    member.ModifiedBy = modifiedBy;
                }
            }

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 })
            {
                ModelState.AddModelError("Team.Name", "The team could not be saved. Check that its name is unique and reload before trying again.");
                return Page();
            }

            if (isBeingDeactivated)
            {
                TempData["SuccessMessage"] = $"Team '{existing.Name}' deactivated. {unassignedCount} member(s) unassigned. Open tickets in group '{existing.Group}' remain - reassign them manually.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Team '{existing.Name}' updated successfully!";
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadCounts()
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == Team.Id);

            if (team != null)
            {
                MemberCount = team.Members.Count(m => m.IsActive);
                OpenTicketCount = await _context.Tickets
                    .CountAsync(t => t.AssignedGroup == team.Group &&
                                    t.Status != TicketStatus.Closed &&
                                    t.Status != TicketStatus.Resolved &&
                                    t.Status != TicketStatus.Cancelled);
            }
        }
    }
}
