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
    public class IndexModel : PageModel
    {
        private readonly WebApplication1.Data.ApplicationDbContext _context;

        public IndexModel(WebApplication1.Data.ApplicationDbContext context)
        {
            _context = context;
        }
        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public WebApplication1.Models.TicketStatus? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public WebApplication1.Models.Severity? Severity { get; set; }

        [BindProperty(SupportsGet = true)]
        public WebApplication1.Models.TicketCategory? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public WebApplication1.Models.AssignmentGroup? Group { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sla { get; set; }

        public IList<Ticket> Ticket { get;set; } = default!;

        public async Task OnGetAsync()
        {
            var query = _context.Tickets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                query = query.Where(t =>
                    t.Title.Contains(Q) ||
                    t.Description.Contains(Q) ||
                    (t.CustomerName != null && t.CustomerName.Contains(Q)) ||
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
        }

    }
}
