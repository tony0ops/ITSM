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

namespace WebApplication1.Pages.Tickets
{
    public class EditModel : PageModel
    {
        private readonly WebApplication1.Data.ApplicationDbContext _context;

        public EditModel(WebApplication1.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket =  await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }
            Ticket = ticket;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Ticket == null) return NotFound();

            var existing = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == Ticket.Id);
            if (existing == null) return NotFound();

            // Remove validation for system fields we don't edit here
            ModelState.Remove("Ticket.Number");
            ModelState.Remove("Ticket.CreatedAt");
            ModelState.Remove("Ticket.ClosedAt");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validate deadline if provided
            if (Ticket.SlaDueAt.HasValue && Ticket.SlaDueAt.Value < existing.CreatedAt)
            {
                ModelState.AddModelError("Ticket.SlaDueAt", "Deadline cannot be earlier than Created time.");
                return Page();
            }

            // Update allowed fields
            existing.Title = Ticket.Title;
            existing.Description = Ticket.Description;
            existing.Severity = Ticket.Severity;
            existing.Status = Ticket.Status;
            existing.CustomerName = Ticket.CustomerName;
            existing.AssigneeUserId = Ticket.AssigneeUserId;

            // SLA logic:
            // - if user set a deadline => keep it
            // - if user cleared it => recalc from severity
            if (Ticket.SlaDueAt.HasValue)
                existing.SlaDueAt = Ticket.SlaDueAt.Value;
            else
                existing.SlaDueAt = existing.CreatedAt + SlaPolicy.GetSlaDuration(existing.Severity);

            // Optional: set/clear ClosedAt automatically (v1 behaviour)
            if (existing.Status is TicketStatus.Resolved or TicketStatus.Closed)
                existing.ClosedAt ??= DateTime.Now;
            else
                existing.ClosedAt = null;

            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }
}
