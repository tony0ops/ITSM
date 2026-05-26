using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Portal
{
    [Authorize]
    public class MyTicketsModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager) : PageModel
    {
        public List<TicketRowViewModel> TicketRows { get; set; } = [];
        public Customer? CustomerInfo { get; set; }
        public string CustomerName { get; set; } = "Your Company";
        public int TotalTicketsCount { get; set; }
        public int TotalComments { get; set; }
        public int OpenTicketsCount { get; set; }
        public int ClosedTicketsCount { get; set; }

        [BindProperty(SupportsGet = true)] public TicketStatus? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? SortBy { get; set; } = "updated_desc";

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
                return RedirectToPage("/Tickets/Index");

            var customerUser = await context.CustomerUsers
                .Include(cu => cu.Customer)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            if (customerUser == null)
            {
                TempData["ErrorMessage"] = "Your account is not linked to a customer. Contact support for assistance.";
                return RedirectToPage("/Index");
            }

            // Block access if the parent customer company is deactivated.
            if (!customerUser.Customer.IsActive)
            {
                TempData["ErrorMessage"] = "Your company account has been deactivated. Contact support for assistance.";
                return RedirectToPage("/Index");
            }

            if (!customerUser.CanViewTickets)
                return Forbid();

            CustomerInfo = customerUser.Customer;
            CustomerName = customerUser.Customer.CompanyName;

            var allTickets = await context.Tickets
                .Include(t => t.Comments)
                .Where(t => t.CustomerId == customerUser.CustomerId)
                .ToListAsync();

            TotalTicketsCount = allTickets.Count;

            // Open = anything not in a terminal state (Closed/Resolved/Cancelled).
            OpenTicketsCount = allTickets.Count(t => !TicketHelper.IsClosedLike(t.Status));

            // Closed = Resolved, Closed, or Cancelled - consistent with IsClosedLike.
            ClosedTicketsCount = allTickets.Count(t => TicketHelper.IsClosedLike(t.Status));

            TotalComments = allTickets
                .SelectMany(t => t.Comments)
                .Count(c => c.IsCustomerVisible && !c.IsInternal);

            var rows = allTickets.Select(t =>
            {
                var lastVisibleComment = t.Comments
                    .Where(c => c.IsCustomerVisible && !c.IsInternal)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefault();

                return new TicketRowViewModel
                {
                    Id             = t.Id,
                    Number         = t.Number,
                    Title          = t.Title,
                    Status         = t.Status,
                    Severity       = t.Severity,
                    Category       = t.Category,
                    CreatedAt      = t.CreatedAt,
                    LastUpdated    = lastVisibleComment?.CreatedAt ?? t.UpdatedAt ?? t.CreatedAt,
                    LastUpdateNote = lastVisibleComment?.Comment
                };
            }).AsQueryable();

            if (StatusFilter.HasValue)
                rows = rows.Where(r => r.Status == StatusFilter.Value);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                rows = rows.Where(r =>
                    r.Number.ToLower().Contains(term) ||
                    r.Title.ToLower().Contains(term));
            }

            TicketRows = SortBy switch
            {
                "updated_asc"  => [.. rows.OrderBy(r => r.LastUpdated)],
                "created_desc" => [.. rows.OrderByDescending(r => r.CreatedAt)],
                "created_asc"  => [.. rows.OrderBy(r => r.CreatedAt)],
                "status"       => [.. rows.OrderBy(r => r.Status)],
                "severity"     => [.. rows.OrderBy(r => r.Severity)],
                _              => [.. rows.OrderByDescending(r => r.LastUpdated)]
            };

            return Page();
        }

        public class TicketRowViewModel
        {
            public int Id { get; set; }
            public string Number { get; set; } = "";
            public string Title { get; set; } = "";
            public TicketStatus Status { get; set; }
            public Severity Severity { get; set; }
            public TicketCategory Category { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastUpdated { get; set; }
            public string? LastUpdateNote { get; set; }
        }
    }
}