using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    public class CreateModel : PageModel
    {
        private readonly WebApplication1.Data.ApplicationDbContext _context;

        public CreateModel(WebApplication1.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // System fields (local time)
            Ticket.CreatedAt = DateTime.Now;

            // INC number
            var nextId = (_context.Tickets.Any() ? _context.Tickets.Max(t => t.Id) : 0) + 1;
            Ticket.Number = $"INC{nextId:0000000}";

            // Validate deadline if provided
            if (Ticket.SlaDueAt.HasValue && Ticket.SlaDueAt.Value < Ticket.CreatedAt)
            {
                ModelState.AddModelError("Ticket.SlaDueAt", "Deadline cannot be earlier than Created time.");
            }

            // Auto-calc SLA only if user didn't set a deadline
            if (!Ticket.SlaDueAt.HasValue)
            {
                Ticket.SlaDueAt = Ticket.CreatedAt + SlaPolicy.GetSlaDuration(Ticket.Severity);
            }

            // Enforce New status on creation
            Ticket.Status = TicketStatus.New;
            Ticket.ClosedAt = null;

            // Remove prior validation errors for system fields
            ModelState.Remove("Ticket.Number");
            ModelState.Remove("Ticket.CreatedAt");
            ModelState.Remove("Ticket.Status");
            ModelState.Remove("Ticket.ClosedAt");

            if (!ModelState.IsValid)
                return Page();

            _context.Tickets.Add(Ticket);
            await _context.SaveChangesAsync();

            // Log ticket creation activity
            var userName = User.Identity?.Name ?? "Anonymous";
            var activity = new TicketActivity
            {
                TicketId = Ticket.Id,
                Type = ActivityType.Created,
                CreatedAt = DateTime.Now,
                UserId = User.Identity?.Name ?? "",
                UserName = userName,
                Description = $"Ticket created: {Ticket.Title}"
            };
            _context.TicketActivities.Add(activity);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}