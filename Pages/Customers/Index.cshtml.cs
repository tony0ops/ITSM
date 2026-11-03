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
        public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 25);
        public Dictionary<int, int> TicketCounts { get; set; } = [];

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Customers.AsNoTracking().AsQueryable();

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

            var page = await PagedResult<Customer>.CreateAsync(query.OrderBy(c => c.CompanyName), PageNumber,
                PageSize, routeValues: PaginationViewModel.ReadRouteValues(Request.Query));
            Pagination = page.Pagination;
            Customers = page.Items;
            var customerIds = Customers.Select(c => c.Id).ToArray();
            TicketCounts = await _context.Tickets.AsNoTracking()
                .Where(t => t.CustomerId.HasValue && customerIds.Contains(t.CustomerId.Value))
                .GroupBy(t => t.CustomerId!.Value)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CustomerId, x => x.Count);
        }

        public int GetTicketCount(int customerId) => TicketCounts.GetValueOrDefault(customerId);
    }
}
