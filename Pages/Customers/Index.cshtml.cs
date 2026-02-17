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
                query = query.Where(c =>
                    c.CompanyName.Contains(SearchTerm) ||
                    (c.PrimaryContactName != null && c.PrimaryContactName.Contains(SearchTerm)) ||
                    (c.PrimaryEmail != null && c.PrimaryEmail.Contains(SearchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                if (StatusFilter.ToLower() == "active")
                    query = query.Where(c => c.IsActive);
                else if (StatusFilter.ToLower() == "inactive")
                    query = query.Where(c => !c.IsActive);
            }

            Customers = await query
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }
    }
}