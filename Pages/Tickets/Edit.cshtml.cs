using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public EditModel(
            ApplicationDbContext context,
            SelectListService selectListService)
        {
            _context = context;
            _selectListService = selectListService;
        }

        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        public SelectList CustomerList { get; set; } = default!;
        public SelectList AssigneeList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            // Load dropdowns
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

            // Remove validation for system fields
            ModelState.Remove("Ticket.Number");
            ModelState.Remove("Ticket.CreatedAt");
            ModelState.Remove("Ticket.ClosedAt");
            ModelState.Remove("Ticket.SlaPausedAt");
            ModelState.Remove("Ticket.TotalPausedDuration");
            ModelState.Remove("Ticket.CustomerName");
            ModelState.Remove("Ticket.Customer");

            if (!ModelState.IsValid)
            {
                // Reload dropdowns on validation error
                CustomerList = _selectListService.GetActiveCustomers();
                AssigneeList = await _selectListService.GetUsersAsync();
                return Page();
            }

            // Validate deadline if provided
            if (Ticket.SlaDueAt.HasValue && Ticket.SlaDueAt.Value < existing.CreatedAt)
            {
                ModelState.AddModelError("Ticket.SlaDueAt", "Deadline cannot be earlier than Created time.");
                CustomerList = _selectListService.GetActiveCustomers();
                AssigneeList = await _selectListService.GetUsersAsync();
                return Page();
            }

            // Track changes for activity log
            var activities = new List<TicketActivity>();
            var userName = User.Identity?.Name ?? "System";

            // Track status change
            if (existing.Status != Ticket.Status)
            {
                var wasSlaPaused = SlaPolicy.IsSlaPaused(existing.Status);
                var willBeSlaPaused = SlaPolicy.IsSlaPaused(Ticket.Status);

                if (!wasSlaPaused && willBeSlaPaused)
                {
                    // Pausing SLA
                    existing.SlaPausedAt = DateTime.Now;
                    existing.PauseReason = Ticket.PauseReason ?? "Waiting for response";

                    activities.Add(new TicketActivity
                    {
                        TicketId = existing.Id,
                        Type = ActivityType.StatusChanged,
                        CreatedAt = DateTime.Now,
                        UserId = User.Identity?.Name ?? "system",
                        UserName = userName,
                        Description = $"SLA paused: {existing.PauseReason}",
                        OldValue = existing.Status.ToString(),
                        NewValue = Ticket.Status.ToString(),
                        IsInternal = true
                    });
                }
                else if (wasSlaPaused && !willBeSlaPaused)
                {
                    // Resuming SLA
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
                            UserId = User.Identity?.Name ?? "system",
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
                    // Regular status change
                    activities.Add(new TicketActivity
                    {
                        TicketId = existing.Id,
                        Type = ActivityType.StatusChanged,
                        CreatedAt = DateTime.Now,
                        UserId = User.Identity?.Name ?? "system",
                        UserName = userName,
                        Description = "Status changed",
                        OldValue = existing.Status.ToString(),
                        NewValue = Ticket.Status.ToString(),
                        IsInternal = false
                    });
                }

                existing.Status = Ticket.Status;
            }

            // Track severity change
            if (existing.Severity != Ticket.Severity)
            {
                activities.Add(new TicketActivity
                {
                    TicketId = existing.Id,
                    Type = ActivityType.SeverityChanged,
                    CreatedAt = DateTime.Now,
                    UserId = User.Identity?.Name ?? "system",
                    UserName = userName,
                    Description = "Severity changed",
                    OldValue = existing.Severity.ToString(),
                    NewValue = Ticket.Severity.ToString(),
                    IsInternal = false
                });
            }

            // Track reassignment
            if (existing.AssigneeUserId != Ticket.AssigneeUserId)
            {
                activities.Add(new TicketActivity
                {
                    TicketId = existing.Id,
                    Type = ActivityType.Reassigned,
                    CreatedAt = DateTime.Now,
                    UserId = User.Identity?.Name ?? "system",
                    UserName = userName,
                    Description = "Ticket reassigned",
                    OldValue = existing.AssigneeUserId ?? "Unassigned",
                    NewValue = Ticket.AssigneeUserId ?? "Unassigned",
                    IsInternal = false
                });
            }

            // Update allowed fields
            existing.Title = Ticket.Title;
            existing.Description = Ticket.Description;
            existing.Severity = Ticket.Severity;
            existing.AssigneeUserId = Ticket.AssigneeUserId;
            existing.AssignedGroup = Ticket.AssignedGroup;
            existing.Category = Ticket.Category;

            // Update customer reference
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

            // SLA logic: keep user-set deadline or auto-calc from severity
            if (Ticket.SlaDueAt.HasValue)
                existing.SlaDueAt = Ticket.SlaDueAt.Value;
            else
                existing.SlaDueAt = existing.CreatedAt + SlaPolicy.GetSlaDuration(existing.Severity);

            // Handle resolution statuses
            if (existing.Status == TicketStatus.Resolved)
            {
                existing.ResolvedAt ??= DateTime.Now;
                existing.ResolutionNotes = Ticket.ResolutionNotes;
            }

            // Set/clear ClosedAt automatically
            if (existing.Status is TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled)
                existing.ClosedAt ??= DateTime.Now;
            else
            {
                existing.ClosedAt = null;
                existing.ResolvedAt = null;
            }

            // Save all activities
            if (activities.Any())
            {
                _context.TicketActivities.AddRange(activities);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = existing.Id });
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }
}