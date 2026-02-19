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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public TicketStatus? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public Severity? Severity { get; set; }

        [BindProperty(SupportsGet = true)]
        public TicketCategory? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public AssignmentGroup? Group { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sla { get; set; }

        public IList<Ticket> Ticket { get; set; } = default!;
        public Dictionary<string, string> UserEmails { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _context.Tickets
                .Include(t => t.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                query = query.Where(t =>
                    t.Title.Contains(Q) ||
                    t.Description.Contains(Q) ||
                    (t.CustomerName != null && t.CustomerName.Contains(Q)) ||
                    (t.Customer != null && t.Customer.CompanyName.Contains(Q)) ||
                    (t.Number != null && t.Number.Contains(Q)));
            }

            if (Status.HasValue)
                query = query.Where(t => t.Status == Status.Value);

            if (Severity.HasValue)
                query = query.Where(t => t.Severity == Severity.Value);

            if (Category.HasValue)
                query = query.Where(t => t.Category == Category.Value);

            if (Group.HasValue)
                query = query.Where(t => t.AssignedGroup == Group.Value);

            var list = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var now = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(Sla))
            {
                var slaKey = Sla.Trim().ToLowerInvariant();

                list = slaKey switch
                {
                    "green" => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Green).ToList(),
                    "yellow" => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Yellow).ToList(),
                    "red" => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Red).ToList(),
                    "breached" => list.Where(t => SlaPolicy.GetState(t, now) == SlaState.Breached).ToList(),
                    "atrisk" => list.Where(t =>
                        {
                            var st = SlaPolicy.GetState(t, now);
                            return st == SlaState.Yellow || st == SlaState.Red || st == SlaState.Breached;
                        }).ToList(),
                    _ => list
                };
            }

            Ticket = list;

            // Load user emails for assigned tickets (optimized: batch load)
            var userIds = Ticket
                .Where(t => !string.IsNullOrEmpty(t.AssigneeUserId))
                .Select(t => t.AssigneeUserId!)
                .Distinct()
                .ToList();

            if (userIds.Any())
            {
                var users = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in users)
                {
                    UserEmails[user.Id] = user.Email ?? user.UserName ?? "Unknown";
                }
            }
        }
    }
}
