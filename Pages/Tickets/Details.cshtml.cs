using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    public class DetailsModel : PageModel
    {
        private readonly WebApplication1.Data.ApplicationDbContext _context;

        public DetailsModel(WebApplication1.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Ticket Ticket { get; set; } = default!;
        public List<TicketActivity> Activities { get; set; } = new();
        public TimeSpan TotalTimeSpent { get; set; }

        [BindProperty]
        public int Hours { get; set; }

        [BindProperty]
        public int Minutes { get; set; }

        [BindProperty]
        public string TimeDescription { get; set; } = "";

        [BindProperty]
        public bool IsInternal { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Activities)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }
            else
            {
                Ticket = ticket;
                Activities = ticket.Activities.OrderByDescending(a => a.CreatedAt).ToList();
                
                // Calculate total time spent
                TotalTimeSpent = TimeSpan.FromMinutes(
                    ticket.Activities
                        .Where(a => a.Type == ActivityType.TimeRegistered && a.TimeSpent.HasValue)
                        .Sum(a => a.TimeSpent!.Value.TotalMinutes)
                );
            }
            return Page();
        }

        public async Task<IActionResult> OnPostLogTimeAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            // Validate time input
            if (Hours < 0 || Minutes < 0)
            {
                ModelState.AddModelError("", "Time values cannot be negative");
                return await OnGetAsync(id);
            }
            
            if (Hours == 0 && Minutes == 0)
            {
                ModelState.AddModelError("", "Time must be greater than 0");
                return await OnGetAsync(id);
            }

            var userName = User.Identity?.Name ?? "Anonymous";
            var userId = User.Identity?.Name ?? "";

            var timeSpent = new TimeSpan(Hours, Minutes, 0);
            var activity = new TicketActivity
            {
                TicketId = ticket.Id,
                Type = ActivityType.TimeRegistered,
                CreatedAt = DateTime.Now,
                UserId = userId,
                UserName = userName,
                TimeSpent = timeSpent,
                Description = TimeDescription,
                IsInternal = IsInternal
            };

            _context.TicketActivities.Add(activity);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = id });
        }
    }
}
