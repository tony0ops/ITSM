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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SelectListService _selectListService;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(
            ApplicationDbContext context,
            SelectListService selectListService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _selectListService = selectListService;
            _userManager = userManager;
        }

        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        public SelectList CustomerList { get; set; } = default!;
        public SelectList AssigneeList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
                return NotFound();

            CustomerList = _selectListService.GetActiveCustomers();
            AssigneeList = await _selectListService.GetUsersAsync();

            Ticket = ticket;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Ticket == null) return NotFound();

            var existing = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == Ticket.Id);
            if (existing == null) return NotFound();

            ModelState.Remove("Ticket.Number");
            ModelState.Remove("Ticket.CreatedAt");
            ModelState.Remove("Ticket.ClosedAt");
            ModelState.Remove("Ticket.SlaPausedAt");
            ModelState.Remove("Ticket.TotalPausedDuration");
            ModelState.Remove("Ticket.CustomerName");
            ModelState.Remove("Ticket.Customer");

            if (!ModelState.IsValid)
            {
                CustomerList = _selectListService.GetActiveCustomers();
                AssigneeList = await _selectListService.GetUsersAsync();
                return Page();
            }

            if (Ticket.SlaDueAt.HasValue && Ticket.SlaDueAt.Value < existing.CreatedAt)
            {
                ModelState.AddModelError("Ticket.SlaDueAt", "Deadline cannot be earlier than Created time.");
                CustomerList = _selectListService.GetActiveCustomers();
                AssigneeList = await _selectListService.GetUsersAsync();
                return Page();
            }

            // Resolve current user identity
            var currentUser = await _userManager.GetUserAsync(User);
            var userId = currentUser?.Id ?? "";
            var userName = currentUser?.Email ?? currentUser?.UserName ?? "System";

            var activities = new List<TicketActivity>();

            // Track status change
            if (existing.Status != Ticket.Status)
            {
                var wasSlaPaused = SlaPolicy.IsSlaPaused(existing.Status);
                var willBeSlaPaused = SlaPolicy.IsSlaPaused(Ticket.Status);

                if (!wasSlaPaused && willBeSlaPaused)
                {
                    existing.SlaPausedAt = DateTime.Now;
                    existing.PauseReason = Ticket.PauseReason ?? "Waiting for response";

                    activities.Add(new TicketActivity
                    {
                        TicketId = existing.Id,
                        Type = ActivityType.StatusChanged,
                        CreatedAt = DateTime.Now,
                        UserId = userId,
                        UserName = userName,
                        Description = $"SLA paused: {existing.PauseReason}",
                        OldValue = existing.Status.ToString(),
                        NewValue = Ticket.Status.ToString(),
                        IsInternal = true
                    });
                }
                else if (wasSlaPaused && !willBeSlaPaused)
                {
                    if (existing.SlaPausedAt.HasValue)
                    {
                        var pauseDuration = DateTime.Now - existing.SlaPausedAt.Value;
                        existing.TotalPausedDuration += pauseDuration;
                        existing.SlaPausedAt = null;
                        existing.PauseReason = null;

                        activities.Add(new TicketActivity
                        {
                            TicketId = existing.Id,
                            Type = ActivityType.StatusChanged,
                            CreatedAt = DateTime.Now,
                            UserId = userId,
                            UserName = userName,
                            Description = $"SLA resumed (was paused for {SlaPolicy.FormatPausedDuration(pauseDuration)})",
                            OldValue = existing.Status.ToString(),
                            NewValue = Ticket.Status.ToString(),
                            IsInternal = true
                        });
                    }
                }
                else
                {
                    activities.Add(new TicketActivity
                    {
                        TicketId = existing.Id,
                        Type = ActivityType.StatusChanged,
                        CreatedAt = DateTime.Now,
                        UserId = userId,
                        UserName = userName,
                        Description = "Status changed",
                        OldValue = existing.Status.ToString(),
                        NewValue = Ticket.Status.ToString(),
                        IsInternal = false
                    });
                }

                existing.Status = Ticket.Status;
            }

            if (existing.Severity != Ticket.Severity)
            {
                activities.Add(new TicketActivity
                {
                    TicketId = existing.Id,
                    Type = ActivityType.SeverityChanged,
                    CreatedAt = DateTime.Now,
                    UserId = userId,
                    UserName = userName,
                    Description = "Severity changed",
                    OldValue = existing.Severity.ToString(),
                    NewValue = Ticket.Severity.ToString(),
                    IsInternal = false
                });
            }

            if (existing.AssigneeUserId != Ticket.AssigneeUserId)
            {
                var oldName = await ResolveUserNameAsync(existing.AssigneeUserId);
                var newName = await ResolveUserNameAsync(Ticket.AssigneeUserId);

                activities.Add(new TicketActivity
                {
                    TicketId = existing.Id,
                    Type = ActivityType.Reassigned,
                    CreatedAt = DateTime.Now,
                    UserId = userId,
                    UserName = userName,
                    Description = "Ticket reassigned",
                    OldValue = oldName,
                    NewValue = newName,
                    IsInternal = false
                });
            }

            existing.Title = Ticket.Title;
            existing.Description = Ticket.Description;
            existing.Severity = Ticket.Severity;
            existing.AssigneeUserId = Ticket.AssigneeUserId;
            existing.AssignedGroup = Ticket.AssignedGroup;
            existing.Category = Ticket.Category;
            existing.UpdatedAt = DateTime.Now;
            existing.ModifiedBy = userId;

            if (Ticket.CustomerId.HasValue && Ticket.CustomerId != existing.CustomerId)
            {
                var customer = await _context.Customers.FindAsync(Ticket.CustomerId.Value);
                if (customer != null)
                {
                    existing.CustomerId = Ticket.CustomerId.Value;
                    existing.CustomerName = customer.CompanyName;
                }
            }
            else if (!Ticket.CustomerId.HasValue && existing.CustomerId.HasValue)
            {
                existing.CustomerId = null;
                existing.CustomerName = null;
            }

            // SLA deadline: track changes and log activity
            var oldSlaDue = existing.SlaDueAt;
            if (Ticket.SlaDueAt.HasValue)
            {
                existing.SlaDueAt = Ticket.SlaDueAt.Value;
            }
            else
            {
                // Recalculate from the NEW severity (already applied above)
                existing.SlaDueAt = existing.CreatedAt + SlaPolicy.GetSlaDuration(existing, _context);
            }

            // Log SLA change if the deadline actually moved
            if (oldSlaDue != existing.SlaDueAt)
            {
                var oldDisplay = oldSlaDue?.ToString("M/d/yyyy HH:mm") ?? "Auto";
                var newDisplay = existing.SlaDueAt?.ToString("M/d/yyyy HH:mm") ?? "None";
                var reason = Ticket.SlaDueAt.HasValue
                    ? "Deadline manually updated"
                    : "Deadline auto-recalculated from severity";

                activities.Add(new TicketActivity
                {
                    TicketId = existing.Id,
                    Type = ActivityType.StatusChanged,
                    CreatedAt = DateTime.Now,
                    UserId = userId,
                    UserName = userName,
                    Description = reason,
                    OldValue = oldDisplay,
                    NewValue = newDisplay,
                    IsInternal = true
                });
            }

            if (existing.Status == TicketStatus.Resolved)
            {
                existing.ResolvedAt ??= DateTime.Now;
                existing.ResolutionNotes = Ticket.ResolutionNotes;
            }

            if (existing.Status is TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled)
                existing.ClosedAt ??= DateTime.Now;
            else
            {
                existing.ClosedAt = null;
                existing.ResolvedAt = null;
            }

            if (activities.Count > 0)
            {
                _context.TicketActivities.AddRange(activities);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = existing.Id });
        }

        private async Task<string> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return "Unassigned";

            var user = await _userManager.FindByIdAsync(userId);
            return user?.Email ?? user?.UserName ?? "Unknown";
        }
    }
}