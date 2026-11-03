using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services;

// Check current activity on every request, before endpoint authorization.
public class InactiveUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db,
        SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(context.User);
            if (user != null)
            {
                var profile = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id);
                var link = await db.CustomerUsers.IgnoreQueryFilters().Include(c => c.Customer)
                    .AsNoTracking().FirstOrDefaultAsync(c => c.UserId == user.Id);
                var active = (profile?.IsActive ?? true) &&
                    (link == null || (link.IsActive && link.Customer.IsActive && !link.Customer.IsDeleted));
                if (!active || await users.IsLockedOutAsync(user))
                {
                    await signIn.SignOutAsync();
                    context.Response.Redirect("/Identity/Account/Login");
                    return;
                }
            }
        }
        await next(context);
    }
}
