using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Customers
{
    [Authorize(Roles = "Manager")]
    public class ManageSlaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageSlaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Customer Customer { get; set; } = default!;

        [BindProperty]
        public CustomerSla Sla { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            Customer = customer;

            var sla = await _context.CustomerSlas
                .FirstOrDefaultAsync(cs => cs.CustomerId == id.Value);

            if (sla == null)
            {
                Sla = new CustomerSla
                {
                    CustomerId = id.Value,
                    CriticalResponseTime = TimeSpan.FromHours(4),
                    HighResponseTime = TimeSpan.FromHours(8),
                    MediumResponseTime = TimeSpan.FromHours(24),
                    LowResponseTime = TimeSpan.FromDays(3),
                    InfoResponseTime = TimeSpan.FromDays(7),
                    IsActive = true
                };
            }
            else
            {
                Sla = sla;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var existingSla = await _context.CustomerSlas
                .FirstOrDefaultAsync(cs => cs.CustomerId == Sla.CustomerId);

            if (existingSla == null)
            {
                _context.CustomerSlas.Add(Sla);
            }
            else
            {
                existingSla.CriticalResponseTime = Sla.CriticalResponseTime;
                existingSla.HighResponseTime = Sla.HighResponseTime;
                existingSla.MediumResponseTime = Sla.MediumResponseTime;
                existingSla.LowResponseTime = Sla.LowResponseTime;
                existingSla.InfoResponseTime = Sla.InfoResponseTime;
                existingSla.IsActive = Sla.IsActive;
                existingSla.Notes = Sla.Notes;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "SLA terms updated successfully!";
            return RedirectToPage("/Customers/Details", new { id = Sla.CustomerId });
        }
    }
}