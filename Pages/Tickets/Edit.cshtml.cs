using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Tickets
{
    public class EditModel(
        ApplicationDbContext context,
        SelectListService selectListService,
        UserManager<IdentityUser> userManager) : PageModel
    {
        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        [BindProperty] public bool KeepDeadline { get; set; }

        public SelectList CustomerList { get; set; } = default!;
        public SelectList AssigneeList { get; set; } = default!;

        public async Task<IActionResult> OnGetAssigneesAsync(AssignmentGroup group)
        {
            if (!ModelState.IsValid || !Enum.IsDefined(group)) return BadRequest();
            if (group == AssignmentGroup.Unassigned) return new JsonResult(Array.Empty<object>());
            var choices = await selectListService.GetUsersByGroupAsync(group);
            return new JsonResult(choices.Select(c => new { value = c.Value, text = c.Text }));
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var ticket = await context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
                return NotFound();

            CustomerList = selectListService.GetActiveCustomers(ticket.CustomerId);
            AssigneeList = await selectListService.GetUsersByGroupAsync(ticket.AssignedGroup);

            Ticket = ticket;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Ticket == null)
                return NotFound();

            var existing = await context.Tickets.FirstOrDefaultAsync(t => t.Id == Ticket.Id);
            if (existing == null)
                return NotFound();

            ModelState.Remove("Ticket.Number");
            ModelState.Remove("Ticket.CreatedAt");
            ModelState.Remove("Ticket.ClosedAt");
            ModelState.Remove("Ticket.SlaPausedAt");
            ModelState.Remove("Ticket.TotalPausedDuration");
            ModelState.Remove("Ticket.CustomerName");
            ModelState.Remove("Ticket.Customer");

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

            if (Ticket.CustomerId.HasValue && Ticket.CustomerId != existing.CustomerId &&
                !await context.Customers.AnyAsync(c => c.Id == Ticket.CustomerId && c.IsActive))
                ModelState.AddModelError("Ticket.CustomerId", "Select an active customer, or keep the existing customer.");

            if (!ModelState.IsValid)
            {
                CustomerList = selectListService.GetActiveCustomers(existing.CustomerId);
                AssigneeList = await selectListService.GetUsersByGroupAsync(Ticket.AssignedGroup);
                return Page();
            }

            if (Ticket.SlaDueAt.HasValue && Ticket.SlaDueAt.Value < existing.CreatedAt)
            {
                ModelState.AddModelError("Ticket.SlaDueAt", "Deadline cannot be earlier than Created time.");
                CustomerList = selectListService.GetActiveCustomers(existing.CustomerId);
                AssigneeList = await selectListService.GetUsersByGroupAsync(Ticket.AssignedGroup);
                return Page();
            }

            var currentUser = await userManager.GetUserAsync(User);
            var userId      = currentUser?.Id ?? "";
            var userName    = currentUser?.Email ?? currentUser?.UserName ?? "System";

            var activities = new List<TicketActivity>();
            var policyChanged = existing.Severity != Ticket.Severity || existing.CustomerId != Ticket.CustomerId;
            var oldResolution = existing.ResolutionNotes;
            var oldStatus = existing.Status;

            if (existing.Status != Ticket.Status)
            {
                var wasSlaPaused    = SlaPolicy.IsSlaPaused(existing.Status);
                var willBeSlaPaused = SlaPolicy.IsSlaPaused(Ticket.Status);

                if (!wasSlaPaused && willBeSlaPaused)
                {
                    existing.SlaPausedAt = DateTime.Now;
                    existing.PauseReason = Ticket.PauseReason ?? "Waiting for response";

                    activities.Add(new TicketActivity
                    {
                        TicketId    = existing.Id, Type = ActivityType.SlaPaused,
                        CreatedAt   = DateTime.Now, UserId = userId, UserName = userName,
                        Description = $"SLA paused: {existing.PauseReason}",
                        OldValue    = existing.Status.ToString(), NewValue = Ticket.Status.ToString(),
                        IsInternal  = true
                    });
                }
                else if (wasSlaPaused && !willBeSlaPaused)
                {
                    if (existing.SlaPausedAt.HasValue)
                    {
                        var pauseDuration = DateTime.Now - existing.SlaPausedAt.Value;
                        existing.TotalPausedDuration += pauseDuration;
                        existing.SlaPausedAt          = null;
                        existing.PauseReason          = null;

                        activities.Add(new TicketActivity
                        {
                            TicketId    = existing.Id, Type = ActivityType.SlaResumed,
                            CreatedAt   = DateTime.Now, UserId = userId, UserName = userName,
                            Description = $"SLA resumed (was paused for {SlaPolicy.FormatPausedDuration(pauseDuration)})",
                            OldValue    = existing.Status.ToString(), NewValue = Ticket.Status.ToString(),
                            IsInternal  = true
                        });
                    }
                }
                activities.Add(new TicketActivity
                {
                    TicketId = existing.Id, Type = ActivityType.StatusChanged,
                    CreatedAt = DateTime.Now, UserId = userId, UserName = userName,
                    Description = "Status changed", OldValue = existing.Status.ToString(), NewValue = Ticket.Status.ToString(),
                    IsInternal = false
                });

                existing.Status = Ticket.Status;
            }

            if (existing.Severity != Ticket.Severity)
            {
                activities.Add(new TicketActivity
                {
                    TicketId    = existing.Id, Type = ActivityType.SeverityChanged,
                    CreatedAt   = DateTime.Now, UserId = userId, UserName = userName,
                    Description = "Severity changed",
                    OldValue    = existing.Severity.ToString(), NewValue = Ticket.Severity.ToString(),
                    IsInternal  = false
                });
            }

            if (existing.AssigneeUserId != Ticket.AssigneeUserId)
            {
                var oldName = await ResolveUserNameAsync(existing.AssigneeUserId);
                var newName = await ResolveUserNameAsync(Ticket.AssigneeUserId);

                activities.Add(new TicketActivity
                {
                    TicketId    = existing.Id, Type = ActivityType.Reassigned,
                    CreatedAt   = DateTime.Now, UserId = userId, UserName = userName,
                    Description = "Ticket reassigned",
                    OldValue    = oldName, NewValue = newName,
                    IsInternal  = false
                });
            }

            if (existing.AssignedGroup != Ticket.AssignedGroup)
                activities.Add(new TicketActivity { TicketId = existing.Id, Type = ActivityType.AssignmentGroupChanged,
                    UserId = userId, UserName = userName, IsInternal = true,
                    Description = $"Assignment group changed from {existing.AssignedGroup} to {Ticket.AssignedGroup}",
                    OldValue = existing.AssignedGroup.ToString(), NewValue = Ticket.AssignedGroup.ToString() });
            if (existing.CustomerId != Ticket.CustomerId)
                activities.Add(new TicketActivity { TicketId = existing.Id, Type = ActivityType.CustomerChanged,
                    UserId = userId, UserName = userName, IsInternal = true, Description = "Customer changed",
                    OldValue = existing.CustomerName ?? "None",
                    NewValue = Ticket.CustomerId.HasValue ? (await context.Customers.FindAsync(Ticket.CustomerId.Value))?.CompanyName : "None" });

            existing.Title          = Ticket.Title;
            existing.Description    = Ticket.Description;
            existing.Severity       = Ticket.Severity;
            existing.AssigneeUserId = Ticket.AssigneeUserId;
            existing.AssignedGroup  = Ticket.AssignedGroup;
            existing.Category       = Ticket.Category;
            existing.UpdatedAt      = DateTime.Now;
            existing.ModifiedBy     = userId;

            if (Ticket.CustomerId.HasValue && Ticket.CustomerId != existing.CustomerId)
            {
                var customer = await context.Customers.FindAsync(Ticket.CustomerId.Value);
                if (customer != null)
                {
                    existing.CustomerId   = Ticket.CustomerId.Value;
                    existing.CustomerName = customer.CompanyName;
                }
            }
            else if (!Ticket.CustomerId.HasValue && existing.CustomerId.HasValue)
            {
                existing.CustomerId   = null;
                existing.CustomerName = null;
            }

            var oldSlaDue  = existing.SlaDueAt;
            // A datetime-local form has minute precision. Preserve seconds on an unchanged value.
            var unchangedDeadline = oldSlaDue.HasValue && Ticket.SlaDueAt.HasValue &&
                oldSlaDue.Value.ToString("yyyy-MM-ddTHH:mm") == Ticket.SlaDueAt.Value.ToString("yyyy-MM-ddTHH:mm");
            var autoDeadline = !Ticket.SlaDueAt.HasValue || (policyChanged && unchangedDeadline && !KeepDeadline);
            existing.SlaDueAt = autoDeadline
                ? existing.CreatedAt + SlaPolicy.GetSlaDuration(existing, context)
                : unchangedDeadline ? oldSlaDue : Ticket.SlaDueAt;

            if (oldSlaDue != existing.SlaDueAt)
            {
                activities.Add(new TicketActivity
                {
                    TicketId    = existing.Id, Type = ActivityType.SlaDeadlineChanged,
                    CreatedAt   = DateTime.Now, UserId = userId, UserName = userName,
                    Description = autoDeadline
                        ? "Deadline recalculated from customer and severity"
                        : "Deadline manually updated",
                    OldValue    = oldSlaDue?.ToString("M/d/yyyy HH:mm") ?? "Auto",
                    NewValue    = existing.SlaDueAt?.ToString("M/d/yyyy HH:mm") ?? "None",
                    IsInternal  = true
                });
            }

            if (TicketHelper.IsClosedLike(existing.Status))
                existing.ResolutionNotes = Ticket.ResolutionNotes?.Trim();
            else
                existing.ResolutionNotes = null;

            if (existing.Status == TicketStatus.Resolved)
            {
                existing.ResolvedAt ??= DateTime.Now;
                existing.ClosedAt = null;
            }
            else if (existing.Status is TicketStatus.Closed or TicketStatus.Cancelled)
            {
                existing.ClosedAt ??= DateTime.Now;
                if (existing.Status == TicketStatus.Cancelled) existing.ResolvedAt = null;
            }
            else
            {
                existing.ClosedAt = null;
                existing.ResolvedAt = null;
            }
            if (oldResolution != existing.ResolutionNotes)
                activities.Add(new TicketActivity { TicketId = existing.Id, Type = ActivityType.ResolutionChanged,
                    UserId = userId, UserName = userName, IsInternal = true,
                    Description = oldStatus != existing.Status && !TicketHelper.IsClosedLike(existing.Status)
                        ? "Ticket reopened; previous resolution retained in history" : "Resolution notes updated",
                    OldValue = oldResolution, NewValue = existing.ResolutionNotes });

            if (activities.Count > 0)
                context.TicketActivities.AddRange(activities);

            await context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = existing.Id });
        }

        private async Task<string> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return "Unassigned";

            var user = await userManager.FindByIdAsync(userId);
            return user?.Email ?? user?.UserName ?? "Unknown";
        }
    }
}
