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
    public class TicketDetailModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager) : PageModel
    {
        public Ticket Ticket { get; set; } = default!;
        public List<TicketComment> VisibleComments { get; set; } = [];
        public bool CanAddComments { get; set; }

        [BindProperty]
        public string NewComment { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
                return RedirectToPage("/Tickets/Details", new { id });

            var customerUser = await context.CustomerUsers
                .Include(cu => cu.Customer)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            if (customerUser == null)
                return Forbid();

            // Block access if the parent customer company has been deactivated.
            if (!customerUser.Customer.IsActive)
            {
                TempData["ErrorMessage"] = "Your company account has been deactivated. Contact support for assistance.";
                return RedirectToPage("/Portal/MyTickets");
            }

            // Enforce CanViewTickets - the list page checks it, but direct URL
            // access to this page would bypass that check without this guard.
            if (!customerUser.CanViewTickets)
                return Forbid();

            var ticket = await context.Tickets
                .Include(t => t.Comments)
                .Include(t => t.Activities)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerUser.CustomerId);

            if (ticket == null)
                return NotFound();

            Ticket         = ticket;
            CanAddComments = customerUser.CanAddComments;

            VisibleComments = [.. ticket.Comments
                .Where(c => c.IsCustomerVisible && !c.IsInternal)
                .OrderBy(c => c.CreatedAt)];

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

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
                return Forbid();

            var customerUser = await context.CustomerUsers
                .Include(cu => cu.Customer)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            // Require both CanViewTickets and CanAddComments on POST.
            // A crafted POST cannot bypass the view permission.
            if (customerUser == null || !customerUser.CanViewTickets || !customerUser.CanAddComments)
                return Forbid();

            // Block POST if the parent customer company has been deactivated.
            if (!customerUser.Customer.IsActive)
                return Forbid();

            var ticket = await context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerUser.CustomerId);

            if (ticket == null)
                return NotFound();

            context.TicketComments.Add(new TicketComment
            {
                TicketId          = ticket.Id,
                UserId            = user.Id,
                UserName          = user.Email ?? user.UserName ?? "Customer",
                UserRole          = "Customer",
                Comment           = NewComment.Trim(),
                IsInternal        = false,
                IsCustomerVisible = true
            });

            await context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }
    }
}