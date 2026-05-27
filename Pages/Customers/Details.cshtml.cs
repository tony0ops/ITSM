using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Customers
{
    [Authorize(Roles = "Manager")]
    public class DetailsModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager) : PageModel
    {
        public Customer Customer { get; set; } = default!;
        public List<TicketRowViewModel> TicketRows { get; set; } = [];
        public List<CustomerUserViewModel> PortalUsers { get; set; } = [];
        public decimal TotalHoursBilled { get; set; }
        public decimal TotalRevenue { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TicketSort { get; set; } = "updated_desc";

        [BindProperty(SupportsGet = true)]
        public TicketStatus? TicketStatus { get; set; }

        [BindProperty]
        public string NewPortalEmail { get; set; } = "";

        [BindProperty]
        public string NewPortalPassword { get; set; } = "";

        [BindProperty]
        public bool NewCanViewTickets { get; set; } = true;

        [BindProperty]
        public bool NewCanAddComments { get; set; } = true;

        // NewCanCreateTickets intentionally omitted - feature not yet implemented.
        // CanCreateTickets is always saved as false until /Portal/CreateTicket exists.

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await context.Customers
                .Include(c => c.Tickets)
                    .ThenInclude(t => t.Activities)
                .Include(c => c.Tickets)
                    .ThenInclude(t => t.Comments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (customer == null)
                return NotFound();

            Customer = customer;

            TotalHoursBilled = (decimal)customer.Tickets
                .SelectMany(t => t.Activities)
                .Where(a => a.Type == ActivityType.TimeRegistered && a.TimeSpent.HasValue)
                .Sum(a => a.TimeSpent!.Value.TotalHours);

            TotalRevenue = TotalHoursBilled * customer.HourlyRate;

            var rows = customer.Tickets.Select(t =>
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
                    CreatedAt      = t.CreatedAt,
                    LastUpdated    = lastVisibleComment?.CreatedAt ?? t.UpdatedAt ?? t.CreatedAt,
                    LastUpdateNote = lastVisibleComment?.Comment
                };
            });

            if (TicketStatus.HasValue)
                rows = rows.Where(r => r.Status == TicketStatus.Value);

            TicketRows = TicketSort switch
            {
                "updated_asc"  => [.. rows.OrderBy(r => r.LastUpdated)],
                "created_desc" => [.. rows.OrderByDescending(r => r.CreatedAt)],
                "created_asc"  => [.. rows.OrderBy(r => r.CreatedAt)],
                "status"       => [.. rows.OrderBy(r => r.Status)],
                "severity"     => [.. rows.OrderBy(r => r.Severity)],
                _              => [.. rows.OrderByDescending(r => r.LastUpdated)]
            };

            await LoadPortalUsersAsync(id.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostCreatePortalUserAsync(int id)
        {
            var customer = await context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            // Email is always required. Password is only required when creating
            // a new account - not when linking an existing Identity user.
            if (string.IsNullOrWhiteSpace(NewPortalEmail))
            {
                TempData["ErrorMessage"] = "Email is required.";
                return RedirectToPage(new { id });
            }

            var existingUser = await userManager.FindByEmailAsync(NewPortalEmail.Trim());

            if (existingUser == null && string.IsNullOrWhiteSpace(NewPortalPassword))
            {
                TempData["ErrorMessage"] = "Password is required when creating a new portal user.";
                return RedirectToPage(new { id });
            }

            if (existingUser != null)
            {
                // Block linking internal staff accounts as portal users.
                var hasStaffProfile = await context.UserProfiles
                    .AnyAsync(up => up.UserId == existingUser.Id);

                var isStaffRole =
                    await userManager.IsInRoleAsync(existingUser, "Manager") ||
                    await userManager.IsInRoleAsync(existingUser, "Agent");

                if (hasStaffProfile || isStaffRole)
                {
                    TempData["ErrorMessage"] = $"'{NewPortalEmail}' is an internal staff account and cannot be linked as a portal user.";
                    return RedirectToPage(new { id });
                }

                // Use IgnoreQueryFilters so links to soft-deleted customers are still
                // detected, preventing a unique index violation on CustomerUsers.UserId.
                var existingLink = await context.CustomerUsers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(cu => cu.UserId == existingUser.Id);

                if (existingLink != null)
                {
                    TempData["ErrorMessage"] = $"'{NewPortalEmail}' is already linked to a customer account.";
                    return RedirectToPage(new { id });
                }

                context.CustomerUsers.Add(new CustomerUser
                {
                    CustomerId       = id,
                    UserId           = existingUser.Id,
                    CanViewTickets   = NewCanViewTickets,
                    CanAddComments   = NewCanAddComments,
                    CanCreateTickets = false,
                    IsActive         = true
                });

                if (!await userManager.IsInRoleAsync(existingUser, "Customer"))
                    await userManager.AddToRoleAsync(existingUser, "Customer");

                await context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Existing user '{NewPortalEmail}' linked to {customer.CompanyName}.";
                return RedirectToPage(new { id });
            }

            var newUser = new IdentityUser
            {
                UserName       = NewPortalEmail.Trim(),
                Email          = NewPortalEmail.Trim(),
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newUser, NewPortalPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Could not create user: {errors}";
                return RedirectToPage(new { id });
            }

            await userManager.AddToRoleAsync(newUser, "Customer");

            context.CustomerUsers.Add(new CustomerUser
            {
                CustomerId       = id,
                UserId           = newUser.Id,
                CanViewTickets   = NewCanViewTickets,
                CanAddComments   = NewCanAddComments,
                CanCreateTickets = false,
                IsActive         = true
            });

            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Portal user '{NewPortalEmail}' created for {customer.CompanyName}. Share the credentials directly.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostTogglePortalUserAsync(int id, int customerUserId)
        {
            var cu = await context.CustomerUsers.FindAsync(customerUserId);
            if (cu == null || cu.CustomerId != id)
                return NotFound();

            cu.IsActive  = !cu.IsActive;
            cu.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = cu.IsActive ? "Portal user activated." : "Portal user deactivated.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemovePortalUserAsync(int id, int customerUserId)
        {
            var cu = await context.CustomerUsers.FindAsync(customerUserId);
            if (cu == null || cu.CustomerId != id)
                return NotFound();

            context.CustomerUsers.Remove(cu);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Portal user unlinked. Their login account still exists but they no longer have access to this customer's tickets.";
            return RedirectToPage(new { id });
        }

        private async Task LoadPortalUsersAsync(int customerId)
        {
            var customerUsers = await context.CustomerUsers
                .Include(cu => cu.User)
                .Where(cu => cu.CustomerId == customerId)
                .OrderBy(cu => cu.User.Email)
                .ToListAsync();

            PortalUsers = [.. customerUsers.Select(cu => new CustomerUserViewModel
            {
                CustomerUserId = cu.Id,
                Email          = cu.User?.Email ?? "Unknown",
                CanViewTickets = cu.CanViewTickets,
                CanAddComments = cu.CanAddComments,
                IsActive       = cu.IsActive,
                CreatedAt      = cu.CreatedAt
            })];
        }

        public class TicketRowViewModel
        {
            public int Id { get; set; }
            public string Number { get; set; } = "";
            public string Title { get; set; } = "";
            public TicketStatus Status { get; set; }
            public Severity Severity { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastUpdated { get; set; }
            public string? LastUpdateNote { get; set; }
        }

        public class CustomerUserViewModel
        {
            public int CustomerUserId { get; set; }
            public string Email { get; set; } = "";
            public bool CanViewTickets { get; set; }
            public bool CanAddComments { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}