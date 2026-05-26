using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    // IDE0290: primary constructor
    public class NewQueueModel(ApplicationDbContext context) : PageModel
    {
        public List<Ticket> Tickets { get; set; } = [];

        public async Task OnGetAsync()
        {
            Tickets = await context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Comments)
                .Include(t => t.Activities)
                .Where(t => t.Status == TicketStatus.New && string.IsNullOrEmpty(t.AssigneeUserId))
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}