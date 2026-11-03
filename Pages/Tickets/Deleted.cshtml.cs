using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets;

[Authorize(Roles = "Manager")]
public class DeletedModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<Ticket> Tickets { get; set; } = [];

    public async Task OnGetAsync()
    {
        Tickets = await context.Tickets.IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.IsDeleted).OrderByDescending(t => t.DeletedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var ticket = await context.Tickets.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
        if (ticket == null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var now = DateTime.Now;
        ticket.IsDeleted = false;
        ticket.DeletedAt = null;
        ticket.DeletedBy = null;
        ticket.UpdatedAt = now;
        ticket.ModifiedBy = user?.Id;
        context.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            Type = ActivityType.TicketRestored,
            CreatedAt = now,
            UserId = user?.Id ?? "",
            UserName = user?.Email ?? user?.UserName ?? "Manager",
            Description = "Ticket restored from deleted tickets"
        });
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Ticket {ticket.Number} restored.";
        return RedirectToPage();
    }
}
