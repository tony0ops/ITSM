using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services.TicketActivity;

namespace WebApplication1.Pages.Tickets
{
    public class DetailsModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        TicketActivityFormatterFactory formatterFactory) : PageModel
    {
        public Ticket Ticket { get; set; } = default!;
        public string AssigneeDisplayName { get; set; } = "Unassigned";
        public List<TicketActivity> Activities { get; set; } = [];
        public List<TicketComment> Comments { get; set; } = [];
        public TimeSpan TotalTimeSpent { get; set; }
        public Dictionary<int, ActivityDisplayInfo> ActivityDisplayMap { get; set; } = [];

        public bool ShowLogTimeError { get; set; }
        public int? EditingCommentId { get; set; }
        public int? EditingActivityId { get; set; }

        [BindProperty] public int Hours { get; set; }
        [BindProperty] public int Minutes { get; set; }
        [BindProperty] public string TimeDescription { get; set; } = "";
        [BindProperty] public bool IsInternal { get; set; }
        [BindProperty] public string NewComment { get; set; } = "";
        [BindProperty] public bool IsInternalComment { get; set; }
        [BindProperty] public string EditCommentText { get; set; } = "";
        [BindProperty] public int EditCommentId { get; set; }
        [BindProperty] public int EditActivityId { get; set; }
        [BindProperty] public string EditActivityDescription { get; set; } = "";
        [BindProperty] public int EditActivityHours { get; set; }
        [BindProperty] public int EditActivityMinutes { get; set; }

        public class ActivityDisplayInfo
        {
            public string FormattedText { get; set; } = "";
            public string Icon { get; set; } = "";
            public string CssClass { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync(int? id, int? editComment, int? editActivity)
        {
            if (id == null)
                return NotFound();

            var ticket = await context.Tickets
                .Include(t => t.Activities)
                .Include(t => t.Comments)
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null)
                return NotFound();

            Ticket = ticket;

            if (!string.IsNullOrEmpty(ticket.AssigneeUserId))
            {
                var assignee = await userManager.FindByIdAsync(ticket.AssigneeUserId);
                AssigneeDisplayName = assignee?.Email ?? assignee?.UserName ?? "Unknown";
            }

            Activities = [.. ticket.Activities.OrderByDescending(a => a.CreatedAt)];
            Comments   = [.. ticket.Comments.OrderBy(c => c.CreatedAt)];

            TotalTimeSpent = TimeSpan.FromMinutes(
                ticket.Activities
                    .Where(a => a.Type == ActivityType.TimeRegistered && a.TimeSpent.HasValue)
                    .Sum(a => a.TimeSpent!.Value.TotalMinutes)
            );

            foreach (var activity in Activities)
            {
                ActivityDisplayMap[activity.Id] = new ActivityDisplayInfo
                {
                    FormattedText = formatterFactory.Format(activity),
                    Icon          = formatterFactory.GetIcon(activity),
                    CssClass      = formatterFactory.GetCssClass(activity)
                };
            }

            if (editComment.HasValue)
            {
                var comment = Comments.FirstOrDefault(c => c.Id == editComment.Value);
                if (comment != null)
                {
                    EditingCommentId = comment.Id;
                    EditCommentText  = comment.Comment;
                    EditCommentId    = comment.Id;
                }
            }

            if (editActivity.HasValue)
            {
                var activity = Activities.FirstOrDefault(a =>
                    a.Id == editActivity.Value && a.Type == ActivityType.TimeRegistered);
                if (activity != null)
                {
                    EditingActivityId       = activity.Id;
                    EditActivityDescription = activity.Description;
                    EditActivityHours       = activity.TimeSpent.HasValue ? (int)activity.TimeSpent.Value.TotalHours : 0;
                    EditActivityMinutes     = activity.TimeSpent.HasValue ? activity.TimeSpent.Value.Minutes : 0;
                    EditActivityId          = activity.Id;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLogTimeAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var ticket = await context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
                return NotFound();

            if (Hours < 0 || Minutes < 0)
            {
                ModelState.AddModelError("", "Time values cannot be negative.");
                ShowLogTimeError = true;
                return await OnGetAsync(id, null, null);
            }

            if (Hours == 0 && Minutes == 0)
            {
                ModelState.AddModelError("", "Time must be greater than 0.");
                ShowLogTimeError = true;
                return await OnGetAsync(id, null, null);
            }

            if (string.IsNullOrWhiteSpace(TimeDescription))
            {
                ModelState.AddModelError("TimeDescription", "Work notes are required when logging time.");
                ShowLogTimeError = true;
                return await OnGetAsync(id, null, null);
            }

            var user     = await userManager.GetUserAsync(User);
            var userName = user?.Email ?? user?.UserName ?? "Anonymous";
            var userId   = user?.Id ?? "";

            context.TicketActivities.Add(new TicketActivity
            {
                TicketId    = ticket.Id,
                Type        = ActivityType.TimeRegistered,
                CreatedAt   = DateTime.Now,
                UserId      = userId,
                UserName    = userName,
                TimeSpent   = new TimeSpan(Hours, Minutes, 0),
                Description = TimeDescription.Trim(),
                IsInternal  = IsInternal
            });

            await context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var ticket = await context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(NewComment))
            {
                ModelState.AddModelError("", "Comment cannot be empty");
                return await OnGetAsync(id, null, null);
            }

            var user     = await userManager.GetUserAsync(User);
            var userName = user?.Email ?? user?.UserName ?? "Anonymous";
            var userId   = user?.Id ?? "";
            var roles    = await userManager.GetRolesAsync(user!);
            var userRole = roles.FirstOrDefault() ?? "Agent";

            context.TicketComments.Add(new TicketComment
            {
                TicketId          = ticket.Id,
                UserId            = userId,
                UserName          = userName,
                UserRole          = userRole,
                Comment           = NewComment.Trim(),
                IsInternal        = IsInternalComment,
                IsCustomerVisible = !IsInternalComment
            });

            await context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostEditCommentAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var comment = await context.TicketComments.FindAsync(EditCommentId);
            if (comment == null)
                return NotFound();

            // Verify comment belongs to the ticket in the route
            if (comment.TicketId != id.Value)
                return Forbid();

            var user = await userManager.GetUserAsync(User);
            if (user == null || comment.UserId != user.Id)
                return Forbid();

            if (string.IsNullOrWhiteSpace(EditCommentText))
            {
                ModelState.AddModelError("", "Comment cannot be empty.");
                return await OnGetAsync(id, EditCommentId, null);
            }

            comment.Comment   = EditCommentText.Trim();
            comment.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment updated.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostEditTimeEntryAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var activity = await context.TicketActivities.FindAsync(EditActivityId);
            if (activity == null || activity.Type != ActivityType.TimeRegistered)
                return NotFound();

            // Verify activity belongs to the ticket in the route
            if (activity.TicketId != id.Value)
                return Forbid();

            var user = await userManager.GetUserAsync(User);
            if (user == null || activity.UserId != user.Id)
                return Forbid();

            if (EditActivityHours < 0 || EditActivityMinutes < 0)
            {
                ModelState.AddModelError("", "Time values cannot be negative.");
                return await OnGetAsync(id, null, EditActivityId);
            }

            if (EditActivityHours == 0 && EditActivityMinutes == 0)
            {
                ModelState.AddModelError("", "Time must be greater than 0.");
                return await OnGetAsync(id, null, EditActivityId);
            }

            if (string.IsNullOrWhiteSpace(EditActivityDescription))
            {
                ModelState.AddModelError("", "Work notes are required.");
                return await OnGetAsync(id, null, EditActivityId);
            }

            activity.TimeSpent   = new TimeSpan(EditActivityHours, EditActivityMinutes, 0);
            activity.Description = EditActivityDescription.Trim();
            activity.UpdatedAt   = DateTime.Now;
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Time entry updated.";
            return RedirectToPage(new { id });
        }
    }
}