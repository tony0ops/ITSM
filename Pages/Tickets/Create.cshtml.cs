using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Tickets
{
    public class CreateModel(
        ApplicationDbContext context,
        SelectListService selectListService,
        UserManager<IdentityUser> userManager) : PageModel
    {
        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        public SelectList CustomerList { get; set; } = default!;
        public SelectList AssigneeList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            CustomerList = selectListService.GetActiveCustomers();
            AssigneeList = await selectListService.GetUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Ticket.CreatedAt = DateTime.Now;

            if (Ticket.SlaDueAt.HasValue && Ticket.SlaDueAt.Value < Ticket.CreatedAt)
                ModelState.AddModelError("Ticket.SlaDueAt", "Deadline cannot be earlier than Created time.");

            if (!Ticket.SlaDueAt.HasValue)
                Ticket.SlaDueAt = Ticket.CreatedAt + SlaPolicy.GetSlaDuration(Ticket, context);

            if (Ticket.CustomerId.HasValue)
            {
                var customer = await context.Customers.FindAsync(Ticket.CustomerId.Value);
                if (customer != null)
                    Ticket.CustomerName = customer.CompanyName;
            }

            var currentUser  = await userManager.GetUserAsync(User);
            Ticket.CreatedBy = currentUser?.Id ?? "";
            Ticket.Status    = TicketStatus.New;

            // Cannot have an assignee when group is Unassigned.
            if (Ticket.AssignedGroup == AssignmentGroup.Unassigned &&
                !string.IsNullOrEmpty(Ticket.AssigneeUserId))
            {
                ModelState.AddModelError("Ticket.AssigneeUserId",
                    "Assignee must be empty when assignment group is Unassigned.");
            }

            // Assignee must belong to the selected group.
            if (!string.IsNullOrEmpty(Ticket.AssigneeUserId) &&
                Ticket.AssignedGroup != AssignmentGroup.Unassigned)
            {
                var validAssignees = await selectListService.GetUsersByGroupAsync(Ticket.AssignedGroup);
                if (!validAssignees.Any(a => a.Value == Ticket.AssigneeUserId))
                    ModelState.AddModelError("Ticket.AssigneeUserId",
                        "Selected assignee does not belong to the chosen assignment group.");
            }

            ModelState.Remove("Ticket.Number");
            ModelState.Remove("Ticket.CreatedAt");
            ModelState.Remove("Ticket.Status");
            ModelState.Remove("Ticket.ClosedAt");
            ModelState.Remove("Ticket.SlaPausedAt");
            ModelState.Remove("Ticket.TotalPausedDuration");
            ModelState.Remove("Ticket.PauseReason");
            ModelState.Remove("Ticket.ResolutionNotes");
            ModelState.Remove("Ticket.ResolvedAt");
            ModelState.Remove("Ticket.CustomerName");
            ModelState.Remove("Ticket.Customer");

            if (!ModelState.IsValid)
            {
                CustomerList = selectListService.GetActiveCustomers();
                AssigneeList = await selectListService.GetUsersByGroupAsync(Ticket.AssignedGroup);
                return Page();
            }

            // Use a unique temporary number to satisfy the unique index
            // while the database Id is not yet known.
            Ticket.Number = $"TMP-{Guid.NewGuid():N}";
            context.Tickets.Add(Ticket);
            await context.SaveChangesAsync();

            // Replace temporary number with the stable INC format.
            Ticket.Number = $"INC{Ticket.Id:0000000}";

            var userName = currentUser?.Email ?? currentUser?.UserName ?? "Anonymous";
            context.TicketActivities.Add(new TicketActivity
            {
                TicketId    = Ticket.Id,
                Type        = ActivityType.Created,
                CreatedAt   = DateTime.Now,
                UserId      = currentUser?.Id ?? "",
                UserName    = userName,
                Description = $"Ticket created: {Ticket.Title}"
            });

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ticket {Ticket.Number} created successfully!";
            return RedirectToPage("./Details", new { id = Ticket.Id });
        }
    }
}