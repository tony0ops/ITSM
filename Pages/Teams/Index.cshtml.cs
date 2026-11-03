using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Teams
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Team> Teams { get; set; } = [];
        public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 25);
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Teams.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                query = query.Where(t => t.Name.Contains(SearchTerm) ||
                    (t.Description != null && t.Description.Contains(SearchTerm)));
            var count = await query.CountAsync();
            Pagination = PaginationViewModel.Create(count, PageNumber, PageSize,
                routeValues: PaginationViewModel.ReadRouteValues(Request.Query));
            Teams = await query.OrderBy(t => t.Name)
                .Skip((Pagination.PageNumber - 1) * Pagination.PageSize).Take(Pagination.PageSize)
                .Include(t => t.Members).ToListAsync();
        }
    }
}
