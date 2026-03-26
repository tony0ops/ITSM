using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Customers
{
    [Authorize(Roles = "Manager")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Customer Customer { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers.FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
                return NotFound();

            Customer = customer;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == Customer.Id);
            if (existing == null)
                return NotFound();

            // Update only user-editable fields — preserve system fields
            existing.CompanyName = Customer.CompanyName;
            existing.PrimaryContactName = Customer.PrimaryContactName;
            existing.PrimaryEmail = Customer.PrimaryEmail;
            existing.PrimaryPhone = Customer.PrimaryPhone;
            existing.EscalationContactName = Customer.EscalationContactName;
            existing.EscalationEmail = Customer.EscalationEmail;
            existing.EscalationPhone = Customer.EscalationPhone;
            existing.Address = Customer.Address;
            existing.City = Customer.City;
            existing.State = Customer.State;
            existing.PostalCode = Customer.PostalCode;
            existing.Country = Customer.Country;
            existing.AccountManager = Customer.AccountManager;
            existing.HourlyRate = Customer.HourlyRate;
            existing.Notes = Customer.Notes;
            existing.IsActive = Customer.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{existing.CompanyName}' updated successfully!";
            return RedirectToPage("./Index");
        }
    }
}