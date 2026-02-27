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
    public class MyTicketsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyTicketsModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Ticket> MyTickets { get; set; } = new();
        public Customer? CustomerInfo { get; set; }
        public string CustomerName { get; set; } = "Your Company";
        public int OpenTicketsCount { get; set; }
        public int ResolvedTicketsCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public TicketStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Find customer linked to this portal user
            var customerUser = await _context.CustomerUsers
                .Include(cu => cu.Customer)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id && cu.IsActive);

            if (customerUser == null)
                return NotFound("You are not linked to any customer account.");

            if (!customerUser.CanViewTickets)
                return Forbid("You do not have permission to view tickets.");

            CustomerInfo = customerUser.Customer;
            CustomerName = customerUser.Customer.CompanyName;

            // Build query for customer's tickets
            var query = _context.Tickets
                .Include(t => t.Comments)
                .Include(t => t.Activities)
                .Where(t => t.CustomerId == customerUser.CustomerId)
                .AsQueryable();

            // Apply status filter
            if (StatusFilter.HasValue)
            {
                query = query.Where(t => t.Status == StatusFilter.Value);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(t =>
                    t.Number.Contains(SearchTerm) ||
                    t.Title.Contains(SearchTerm) ||
                    t.Description.Contains(SearchTerm));
            }

            MyTickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Calculate statistics
            var allCustomerTickets = await _context.Tickets
                .Where(t => t.CustomerId == customerUser.CustomerId)
                .ToListAsync();

            OpenTicketsCount = allCustomerTickets.Count(t =>
                t.Status != TicketStatus.Closed &&
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Cancelled);

            ResolvedTicketsCount = allCustomerTickets.Count(t =>
                t.Status == TicketStatus.Closed ||
                t.Status == TicketStatus.Resolved);

            return Page();
        }

        /// <summary>
        /// Get customer-visible comments only
        /// </summary>
        public List<TicketComment> GetVisibleComments(int ticketId)
        {
            return MyTickets
                .FirstOrDefault(t => t.Id == ticketId)?
                .Comments
                .Where(c => c.IsCustomerVisible && !c.IsInternal)
                .OrderBy(c => c.CreatedAt)
                .ToList() ?? new();
        }
    }
}