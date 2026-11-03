using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Teams
{
    [Authorize(Roles = "Manager")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Team Team { get; set; } = new();

        public IActionResult OnGet()
        {
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
                return Page();

            _context.Teams.Add(Team);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 })
            {
                ModelState.AddModelError("Team.Name", "The team could not be saved. Check that its name is unique and reload before trying again.");
                return Page();
            }

            TempData["SuccessMessage"] = $"Team '{Team.Name}' created successfully!";
            return RedirectToPage("./Index");
        }
    }
}
