using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Customers
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Customer> Customers { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Customers
                .Include(c => c.Tickets)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                query = query.Where(c =>
                    c.CompanyName.ToLower().Contains(term) ||
                    (c.PrimaryContactName != null && c.PrimaryContactName.ToLower().Contains(term)) ||
                    (c.PrimaryEmail != null && c.PrimaryEmail.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                if (StatusFilter.Equals("active", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.IsActive);
                else if (StatusFilter.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => !c.IsActive);
            }

            Customers = await query
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }
    }
}