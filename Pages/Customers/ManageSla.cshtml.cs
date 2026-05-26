using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Customers
{
    [Authorize(Roles = "Manager")]
    public class ManageSlaModel(ApplicationDbContext context) : PageModel
    {
        public Customer Customer { get; set; } = default!;

        [BindProperty]
        public ManageSlaViewModel Form { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            Customer = customer;

            var sla = await context.CustomerSlas
                .FirstOrDefaultAsync(cs => cs.CustomerId == id.Value);

            Form = sla == null
                ? new ManageSlaViewModel { CustomerId = id.Value }
                : new ManageSlaViewModel
                {
                    Id            = sla.Id,
                    CustomerId    = sla.CustomerId,
                    CriticalHours = (int)sla.CriticalResponseTime.TotalHours,
                    HighHours     = (int)sla.HighResponseTime.TotalHours,
                    MediumHours   = (int)sla.MediumResponseTime.TotalHours,
                    LowDays       = (int)sla.LowResponseTime.TotalDays,
                    InfoDays      = (int)sla.InfoResponseTime.TotalDays,
                    IsActive      = sla.IsActive,
                    Notes         = sla.Notes
                };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Reload Customer before any early return so the view can render.
            var customer = await context.Customers.FindAsync(Form.CustomerId);
            if (customer == null)
                return NotFound();

            Customer = customer;

            if (!ModelState.IsValid)
                return Page();

            var existingSla = await context.CustomerSlas
                .FirstOrDefaultAsync(cs => cs.CustomerId == Form.CustomerId);

            if (existingSla == null)
            {
                context.CustomerSlas.Add(new CustomerSla
                {
                    CustomerId           = Form.CustomerId,
                    CriticalResponseTime = TimeSpan.FromHours(Form.CriticalHours),
                    HighResponseTime     = TimeSpan.FromHours(Form.HighHours),
                    MediumResponseTime   = TimeSpan.FromHours(Form.MediumHours),
                    LowResponseTime      = TimeSpan.FromDays(Form.LowDays),
                    InfoResponseTime     = TimeSpan.FromDays(Form.InfoDays),
                    IsActive             = Form.IsActive,
                    Notes                = Form.Notes
                });
            }
            else
            {
                existingSla.CriticalResponseTime = TimeSpan.FromHours(Form.CriticalHours);
                existingSla.HighResponseTime     = TimeSpan.FromHours(Form.HighHours);
                existingSla.MediumResponseTime   = TimeSpan.FromHours(Form.MediumHours);
                existingSla.LowResponseTime      = TimeSpan.FromDays(Form.LowDays);
                existingSla.InfoResponseTime      = TimeSpan.FromDays(Form.InfoDays);
                existingSla.IsActive             = Form.IsActive;
                existingSla.Notes                = Form.Notes;
                existingSla.UpdatedAt            = DateTime.Now;
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "SLA terms updated successfully!";
            return RedirectToPage("/Customers/Details", new { id = Form.CustomerId });
        }

        public class ManageSlaViewModel
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }

            [Range(1, 168, ErrorMessage = "Must be between 1 and 168 hours.")]
            public int CriticalHours { get; set; } = 4;

            [Range(1, 168, ErrorMessage = "Must be between 1 and 168 hours.")]
            public int HighHours { get; set; } = 8;

            [Range(1, 168, ErrorMessage = "Must be between 1 and 168 hours.")]
            public int MediumHours { get; set; } = 24;

            [Range(1, 90, ErrorMessage = "Must be between 1 and 90 days.")]
            public int LowDays { get; set; } = 3;

            [Range(1, 90, ErrorMessage = "Must be between 1 and 90 days.")]
            public int InfoDays { get; set; } = 7;

            public bool IsActive { get; set; } = true;

            [StringLength(500)]
            public string? Notes { get; set; }
        }
    }
}