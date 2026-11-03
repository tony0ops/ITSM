using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Portal
{
    [Authorize]
    public class TicketDetailModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        TicketAttachmentStorage attachmentStorage) : PageModel
    {
        public Ticket Ticket { get; set; } = default!;
        public List<TicketComment> VisibleComments { get; set; } = [];
        public List<TicketAttachment> VisibleAttachments { get; set; } = [];
        public bool CanAddComments { get; set; }
        public bool CanPostComment { get; set; }

        [BindProperty]
        public string NewComment { get; set; } = "";
        [BindProperty] public IFormFile? AttachmentFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
                return RedirectToPage("/Tickets/Details", new { id });

            var customerUser = await context.CustomerUsers
                .Include(cu => cu.Customer)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            if (customerUser == null)
                return Forbid();

            // Block access if the parent customer company has been deactivated.
            if (!customerUser.Customer.IsActive)
            {
                TempData["ErrorMessage"] = "Your company account has been deactivated. Contact support for assistance.";
                return RedirectToPage("/Portal/MyTickets");
            }

            // Enforce CanViewTickets - the list page checks it, but direct URL
            // access to this page would bypass that check without this guard.
            if (!customerUser.CanViewTickets)
                return Forbid();

            var ticket = await context.Tickets
                .AsSplitQuery()
                .Include(t => t.Comments.Where(c => c.IsCustomerVisible && !c.IsInternal))
                    .ThenInclude(c => c.Attachments.Where(a => a.IsCustomerVisible))
                .Include(t => t.Activities)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerUser.CustomerId);

            if (ticket == null)
                return NotFound();

            Ticket         = ticket;
            CanAddComments = customerUser.CanAddComments;
            CanPostComment = CanAddComments && TicketConversationFlow.CanPostPublicReply(ticket.Status);

            VisibleComments = [.. ticket.Comments
                .Where(c => c.IsCustomerVisible && !c.IsInternal)
                .OrderBy(c => c.CreatedAt)];
            VisibleAttachments = await context.TicketAttachments.AsNoTracking()
                .Where(a => a.TicketId == ticket.Id && a.IsCustomerVisible && a.TicketCommentId == null)
                .OrderByDescending(a => a.CreatedAt).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadAttachmentAsync(int id, int attachmentId)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
                return RedirectToPage("/Tickets/Details", new { id });

            var customerUser = await context.CustomerUsers.Include(cu => cu.Customer)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);
            if (customerUser == null || !customerUser.CanViewTickets || !customerUser.Customer.IsActive)
                return Forbid();
            var attachment = await context.TicketAttachments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == id &&
                    a.IsCustomerVisible && a.Ticket.CustomerId == customerUser.CustomerId &&
                    (a.TicketCommentId == null ||
                     (a.TicketComment != null && a.TicketComment.IsCustomerVisible && !a.TicketComment.IsInternal)));
            if (attachment == null) return NotFound();
            var stream = attachmentStorage.OpenRead(attachment.StoredFileName);
            if (stream == null) return NotFound();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(stream, attachment.ContentType, attachment.FileName);
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int? id)
        {
            if (id == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(NewComment) || NewComment.Trim().Length > 4000)
            {
                ModelState.AddModelError("", string.IsNullOrWhiteSpace(NewComment)
                    ? "Comment cannot be empty"
                    : "Comments cannot exceed 4,000 characters.");
                return await OnGetAsync(id);
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
                return Forbid();

            var customerUser = await context.CustomerUsers
                .Include(cu => cu.Customer)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            // Require both CanViewTickets and CanAddComments on POST.
            // A crafted POST cannot bypass the view permission.
            if (customerUser == null || !customerUser.CanViewTickets || !customerUser.CanAddComments ||
                !customerUser.Customer.IsActive)
                return Forbid();

            var ticket = await context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerUser.CustomerId);

            if (ticket == null)
                return NotFound();

            if (!TicketConversationFlow.CanPostPublicReply(ticket.Status))
            {
                TempData["ErrorMessage"] = "This ticket is closed or cancelled. Please create a new ticket if you need more help.";
                return RedirectToPage(new { id });
            }

            var userName = user.Email ?? user.UserName ?? "Customer";
            if (userName.Length > 100) userName = userName[..100];
            var commentText = NewComment.Trim();
            var now = DateTime.Now;
            StoredAttachment? saved = null;
            if (AttachmentFile != null)
            {
                try
                {
                    saved = await attachmentStorage.SaveAsync(AttachmentFile);
                }
                catch (TicketAttachmentValidationException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToPage(new { id });
                }
            }

            var comment = new TicketComment
            {
                TicketId = ticket.Id,
                CreatedAt = now,
                UserId = user.Id,
                UserName = userName,
                UserRole = "Customer",
                Comment = commentText,
                IsInternal = false,
                IsCustomerVisible = true
            };
            context.TicketComments.Add(comment);
            if (saved != null)
            {
                context.TicketAttachments.Add(new TicketAttachment
                {
                    TicketId = ticket.Id,
                    TicketComment = comment,
                    FileName = saved.FileName,
                    StoredFileName = saved.StoredFileName,
                    ContentType = saved.ContentType,
                    FileSize = saved.FileSize,
                    UploadedByUserId = user.Id,
                    UploadedByName = userName,
                    UploadedByRole = "Customer",
                    IsCustomerVisible = true
                });
            }
            context.TicketActivities.Add(new TicketActivity
            {
                TicketId = ticket.Id,
                Type = ActivityType.CommentAdded,
                CreatedAt = now,
                UserId = user.Id,
                UserName = userName,
                Description = "Customer replied",
                NewValue = commentText.Length > 450 ? commentText[..450] : commentText,
                IsInternal = false
            });
            if (saved != null)
            {
                context.TicketActivities.Add(new TicketActivity
                {
                    TicketId = ticket.Id,
                    Type = ActivityType.AttachmentUploaded,
                    CreatedAt = now,
                    UserId = user.Id,
                    UserName = userName,
                    Description = $"Customer uploaded attachment with reply: {saved.FileName}",
                    IsInternal = false
                });
            }
            context.TicketActivities.AddRange(
                TicketConversationFlow.ApplyPublicReply(ticket, user.Id, userName, now));
            ticket.UpdatedAt = now;
            ticket.ModifiedBy = user.Id;

            try
            {
                await context.SaveChangesAsync();
            }
            catch
            {
                if (saved != null) await attachmentStorage.DeleteAsync(saved.StoredFileName);
                throw;
            }
            return RedirectToPage(new { id });
        }
    }
}
