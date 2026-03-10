using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SelectListService _selectListService;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(
            ApplicationDbContext context,
            SelectListService selectListService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _selectListService = selectListService;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            CustomerList = _selectListService.GetActiveCustomers();
            AssigneeList = await _selectListService.GetUsersAsync();
            return Page();
        }

        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        public SelectList CustomerList { get; set; } = default!;
        public SelectList AssigneeList { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // System fields
            Ticket.CreatedAt = DateTime.Now;

            // INC number — use IgnoreQueryFilters to include soft-deleted tickets
            var maxId = await _context.Tickets
                .IgnoreQueryFilters()
                .OrderByDescending(t => t.Id)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();
            Ticket.Number = $"INC{maxId + 1:0000000}";

            // Validate deadline if provided
            if (Ticket.SlaDueAt.HasValue && Ticket.SlaDueAt.Value < Ticket.CreatedAt)
            {
                ModelState.AddModelError("Ticket.SlaDueAt", "Deadline cannot be earlier than Created time.");
            }

            // Auto-calc SLA only if user didn't set a deadline
            if (!Ticket.SlaDueAt.HasValue)
            {
                Ticket.SlaDueAt = Ticket.CreatedAt + SlaPolicy.GetSlaDuration(Ticket, _context);
            }

            // Set CustomerName from Customer for backward compatibility
            if (Ticket.CustomerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(Ticket.CustomerId.Value);
                if (customer != null)
                {
                    Ticket.CustomerName = customer.CompanyName;
                }
            }

            // Set audit field
            var currentUser = await _userManager.GetUserAsync(User);
            Ticket.CreatedBy = currentUser?.Id ?? "";

            // Enforce New status on creation
            Ticket.Status = TicketStatus.New;
            Ticket.ClosedAt = null;

            // Remove validation errors for system fields
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
                CustomerList = _selectListService.GetActiveCustomers();
                AssigneeList = await _selectListService.GetUsersAsync();
                return Page();
            }

            _context.Tickets.Add(Ticket);
            await _context.SaveChangesAsync();

            // Log ticket creation activity with real user ID
            var userName = currentUser?.Email ?? currentUser?.UserName ?? "Anonymous";
            var activity = new TicketActivity
            {
                TicketId = Ticket.Id,
                Type = ActivityType.Created,
                CreatedAt = DateTime.Now,
                UserId = currentUser?.Id ?? "",
                UserName = userName,
                Description = $"Ticket created: {Ticket.Title}"
            };
            _context.TicketActivities.Add(activity);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}