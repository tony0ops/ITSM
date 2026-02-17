using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Customers
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Customer Customer { get; set; } = default!;
        public List<Ticket> Tickets { get; set; } = new();
        public decimal TotalHoursBilled { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.Tickets)
                    .ThenInclude(t => t.Activities)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            Customer = customer;
            Tickets = customer.Tickets.OrderByDescending(t => t.CreatedAt).ToList();

            // Calculate billing metrics
            TotalHoursBilled = (decimal)customer.Tickets
                .SelectMany(t => t.Activities)
                .Where(a => a.Type == ActivityType.TimeRegistered && a.TimeSpent.HasValue)
                .Sum(a => a.TimeSpent!.Value.TotalHours);

            TotalRevenue = TotalHoursBilled * customer.HourlyRate;

            return Page();
        }
    }
}