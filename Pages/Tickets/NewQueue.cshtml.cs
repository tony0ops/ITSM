using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    public class NewQueueModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NewQueueModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = [];

        public async Task OnGetAsync()
        {
            // Show all New tickets without an individual assignee
            Tickets = await _context.Tickets
                .Include(t => t.Customer)
                .Where(t => t.Status == TicketStatus.New &&
                            (t.AssigneeUserId == null || t.AssigneeUserId == ""))
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}