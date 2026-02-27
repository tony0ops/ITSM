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

        public async Task OnGetAsync()
        {
            Teams = await _context.Teams
                .Include(t => t.Members)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}