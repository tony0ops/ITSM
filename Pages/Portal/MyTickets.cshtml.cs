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
        public bool CanCreateTickets { get; set; }
        public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 25);

        [BindProperty(SupportsGet = true)] public TicketStatus? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? SortBy { get; set; } = "updated_desc";
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

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
                // Index routes signed-in non-staff back here; forbid to avoid a redirect loop.
                return Forbid();
            }

            // Block access if the parent customer company is deactivated.
            if (!customerUser.Customer.IsActive)
            {
                TempData["ErrorMessage"] = "Your company account has been deactivated. Contact support for assistance.";
                return Forbid();
            }

            if (!customerUser.CanViewTickets)
                return Forbid();

            CustomerInfo = customerUser.Customer;
            CustomerName = customerUser.Customer.CompanyName;
            CanCreateTickets = customerUser.CanViewTickets && customerUser.CanCreateTickets;

            var companyTickets = context.Tickets.AsNoTracking()
                .Where(t => t.CustomerId == customerUser.CustomerId);
            TotalTicketsCount = await companyTickets.CountAsync();
            OpenTicketsCount = await companyTickets.CountAsync(t =>
                t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Cancelled);
            ClosedTicketsCount = TotalTicketsCount - OpenTicketsCount;
            TotalComments = await context.TicketComments.AsNoTracking()
                .CountAsync(c => c.Ticket.CustomerId == customerUser.CustomerId && c.IsCustomerVisible && !c.IsInternal);

            var rows = from t in companyTickets
                       let lastCommentTime = context.TicketComments.AsNoTracking()
                           .Where(c => c.TicketId == t.Id && c.IsCustomerVisible && !c.IsInternal)
                           .Select(c => (DateTime?)(c.UpdatedAt.HasValue && c.UpdatedAt.Value > c.CreatedAt
                               ? c.UpdatedAt.Value : c.CreatedAt)).Max()
                       select new TicketRowViewModel
                       {
                           Id = t.Id,
                           Number = t.Number,
                           Title = t.Title,
                           Status = t.Status,
                           Severity = t.Severity,
                           Category = t.Category,
                           CreatedAt = t.CreatedAt,
                           LastUpdated = lastCommentTime.HasValue && lastCommentTime.Value > (t.UpdatedAt ?? t.CreatedAt)
                               ? lastCommentTime.Value : (t.UpdatedAt ?? t.CreatedAt),
                           LastUpdateNote = context.TicketComments.AsNoTracking()
                               .Where(c => c.TicketId == t.Id && c.IsCustomerVisible && !c.IsInternal)
                               .OrderByDescending(c => c.CreatedAt).Select(c => c.Comment).FirstOrDefault()
                       };

            if (StatusFilter.HasValue)
                rows = rows.Where(r => r.Status == StatusFilter.Value);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                rows = rows.Where(r =>
                    r.Number.ToLower().Contains(term) ||
                    r.Title.ToLower().Contains(term));
            }

            var orderedRows = SortBy switch
            {
                "updated_asc" => rows.OrderBy(r => r.LastUpdated),
                "created_desc" => rows.OrderByDescending(r => r.CreatedAt),
                "created_asc" => rows.OrderBy(r => r.CreatedAt),
                "status" => rows.OrderBy(r => r.Status),
                "severity" => rows.OrderBy(r => r.Severity),
                _ => rows.OrderByDescending(r => r.LastUpdated)
            };
            var page = await PagedResult<TicketRowViewModel>.CreateAsync(orderedRows, PageNumber, PageSize,
                routeValues: PaginationViewModel.ReadRouteValues(Request.Query));
            TicketRows = page.Items;
            Pagination = page.Pagination;

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
