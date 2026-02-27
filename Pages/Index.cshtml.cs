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
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Dashboard/Index");
            }

            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
    }
}
