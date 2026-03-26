using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Portal
{
    [Authorize]
    public class TicketDetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TicketDetailModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Ticket Ticket { get; set; } = default!;
        public List<TicketComment> VisibleComments { get; set; } = new();

        [BindProperty]
        public string NewComment { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Internal staff should use /Tickets/Details
            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
            {
                return RedirectToPage("/Tickets/Details", new { id });
            }

            // Verify user has access to this ticket
            var customerUser = await _context.CustomerUsers
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            if (customerUser == null)
                return Forbid();

            var ticket = await _context.Tickets
                .Include(t => t.Comments)
                .Include(t => t.Activities)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerUser.CustomerId);

            if (ticket == null)
                return NotFound();

            Ticket = ticket;
            VisibleComments = ticket.Comments
                .Where(c => c.IsCustomerVisible && !c.IsInternal)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int? id)
        {
            if (id == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(NewComment))
            {
                ModelState.AddModelError("", "Comment cannot be empty");
                return await OnGetAsync(id);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Block internal staff from posting via portal
            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
                return Forbid();

            // Verify user has access and permission
            var customerUser = await _context.CustomerUsers
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            if (customerUser == null || !customerUser.CanAddComments)
                return Forbid();

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerUser.CustomerId);

            if (ticket == null)
                return NotFound();

            var comment = new TicketComment
            {
                TicketId = ticket.Id,
                UserId = user.Id,
                UserName = user.Email ?? user.UserName ?? "Customer",
                UserRole = "Customer",
                Comment = NewComment.Trim(),
                IsInternal = false,
                IsCustomerVisible = true
            };

            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }
    }
}