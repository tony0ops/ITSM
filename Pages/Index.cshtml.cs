using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Route internal staff to dashboard, customer users to portal
            if (User.IsInRole("Manager") || User.IsInRole("Agent"))
            {
                return RedirectToPage("/Dashboard/Index");
            }

            return RedirectToPage("/Portal/MyTickets");
        }
    }
}
