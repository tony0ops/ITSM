using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Tickets
{
    [Authorize(Roles = "Manager")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EntityLifecycleService _lifecycle;
        private readonly UserManager<IdentityUser> _userManager;

        public DeleteModel(
            ApplicationDbContext context,
            EntityLifecycleService lifecycle,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _lifecycle = lifecycle;
            _userManager = userManager;
        }

        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
                return NotFound();

            Ticket = ticket;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var deletedBy = currentUser?.Id ?? "";
            await _lifecycle.SoftDeleteAsync<Ticket>(id.Value, deletedBy);

            return RedirectToPage("./Index");
        }
    }
}