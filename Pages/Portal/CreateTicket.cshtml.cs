using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Portal;

[Authorize]
public class CreateTicketModel(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager) : PageModel
{
    public const string DisabledMessage = "Ticket creation is not enabled for your account.";

    [BindProperty]
    public TicketInput Input { get; set; } = new();

    public string CustomerName { get; private set; } = "Your Company";

    public async Task<IActionResult> OnGetAsync()
    {
        var access = await GetPortalAccessAsync();
        if (access.Result != null)
            return access.Result;

        CustomerName = access.CustomerUser!.Customer.CompanyName;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var access = await GetPortalAccessAsync();
        if (access.Result != null)
            return access.Result;

        var customerUser = access.CustomerUser!;
        var user = access.User!;

        Input.Title = Input.Title?.Trim() ?? "";
        Input.Description = Input.Description?.Trim() ?? "";
        if (!Enum.IsDefined(Input.Severity))
            ModelState.AddModelError("Input.Severity", "Select a valid severity.");
        if (!Enum.IsDefined(Input.Category))
            ModelState.AddModelError("Input.Category", "Select a valid category.");

        if (!ModelState.IsValid)
        {
            CustomerName = customerUser.Customer.CompanyName;
            return Page();
        }

        var now = DateTime.Now;
        var ticket = new Ticket
        {
            Number = $"TMP-{Guid.NewGuid():N}",
            Title = Input.Title,
            Description = Input.Description,
            Severity = Input.Severity,
            Category = Input.Category,
            Status = TicketStatus.New,
            AssignedGroup = AssignmentGroup.Unassigned,
            CustomerId = customerUser.CustomerId,
            CustomerName = customerUser.Customer.CompanyName,
            CreatedBy = user.Id,
            CreatedAt = now
        };
        ticket.SlaDueAt = now + SlaPolicy.GetSlaDuration(ticket, context);

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        ticket.Number = $"INC{ticket.Id:0000000}";
        context.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            Type = ActivityType.Created,
            UserId = user.Id,
            UserName = user.Email ?? user.UserName ?? "Customer",
            Description = "Ticket submitted through the customer portal",
            IsInternal = false,
            CreatedAt = now
        });
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Ticket {ticket.Number} was submitted.";
        return RedirectToPage("./TicketDetail", new { id = ticket.Id });
    }

    private async Task<PortalAccess> GetPortalAccessAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return new PortalAccess(null, null, Unauthorized());

        if (User.IsInRole("Manager") || User.IsInRole("Agent"))
            return new PortalAccess(user, null, RedirectToPage("/Tickets/Create"));

        var customerUser = await context.CustomerUsers
            .Include(cu => cu.Customer)
            .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

        if (customerUser == null || !customerUser.Customer.IsActive ||
            !customerUser.CanViewTickets || !customerUser.CanCreateTickets)
            return new PortalAccess(user, null, Forbid());

        return new PortalAccess(user, customerUser, null);
    }

    private sealed record PortalAccess(IdentityUser? User, CustomerUser? CustomerUser, IActionResult? Result);

    public sealed class TicketInput
    {
        [Required, StringLength(120)]
        [Display(Name = "Subject")]
        public string Title { get; set; } = "";

        [Required, StringLength(4000)]
        [Display(Name = "What do you need help with?")]
        public string Description { get; set; } = "";

        [Display(Name = "Severity")]
        public Severity Severity { get; set; } = Severity.Medium;

        [Display(Name = "Category")]
        public TicketCategory Category { get; set; } = TicketCategory.GeneralRequest;
    }
}
