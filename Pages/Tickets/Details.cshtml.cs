using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Tickets
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Ticket Ticket { get; set; } = default!;
        public List<TicketActivity> Activities { get; set; } = new();
        public List<TicketComment> Comments { get; set; } = new();
        public TimeSpan TotalTimeSpent { get; set; }

        [BindProperty]
        public int Hours { get; set; }

        [BindProperty]
        public int Minutes { get; set; }

        [BindProperty]
        public string TimeDescription { get; set; } = "";

        [BindProperty]
        public bool IsInternal { get; set; }

        [BindProperty]
        public string NewComment { get; set; } = "";

        [BindProperty]
        public bool IsInternalComment { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Activities)
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (ticket == null)
            {
                return NotFound();
            }

            Ticket = ticket;
            Activities = ticket.Activities.OrderByDescending(a => a.CreatedAt).ToList();
            Comments = ticket.Comments.OrderBy(c => c.CreatedAt).ToList();
            
            // Calculate total time spent
            TotalTimeSpent = TimeSpan.FromMinutes(
                ticket.Activities
                    .Where(a => a.Type == ActivityType.TimeRegistered && a.TimeSpent.HasValue)
                    .Sum(a => a.TimeSpent!.Value.TotalMinutes)
            );

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

            var user = await _userManager.GetUserAsync(User);
            var userName = user?.Email ?? user?.UserName ?? "Anonymous";
            var userId = user?.Id ?? "";

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

        public async Task<IActionResult> OnPostAddCommentAsync(int? id)
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

            if (string.IsNullOrWhiteSpace(NewComment))
            {
                ModelState.AddModelError("", "Comment cannot be empty");
                return await OnGetAsync(id);
            }

            var user = await _userManager.GetUserAsync(User);
            var userName = user?.Email ?? user?.UserName ?? "Anonymous";
            var userId = user?.Id ?? "";

            // Determine user role - you can enhance this based on actual role assignments
            var userRole = "Agent"; // Default to Agent; can be enhanced to check actual roles

            var comment = new TicketComment
            {
                TicketId = ticket.Id,
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Comment = NewComment.Trim(),
                IsInternal = IsInternalComment,
                IsCustomerVisible = !IsInternalComment
            };

            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = id });
        }
    }
}
