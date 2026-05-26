using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;

namespace WebApplication1.Services;

/// <summary>
/// Signs out users whose staff profile or customer portal account has been
/// deactivated. Also signs out portal users whose parent customer is inactive.
/// Uses a short-lived cache to avoid a database query on every request.
/// </summary>
public class InactiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public InactiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                var cacheKey = $"UserActive_{user.Id}";

                if (!cache.TryGetValue(cacheKey, out bool isActive))
                {
                    var profile = await dbContext.UserProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);

                    if (profile != null)
                    {
                        // Internal staff - enforce staff profile active flag.
                        isActive = profile.IsActive;
                    }
                    else
                    {
                        // Customer portal user - check both the portal user record
                        // and the parent customer active status.
                        var customerUser = await dbContext.CustomerUsers
                            .AsNoTracking()
                            .Include(cu => cu.Customer)
                            .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

                        isActive = customerUser != null
                            && customerUser.IsActive
                            && customerUser.Customer != null
                            && customerUser.Customer.IsActive;

                        // If no profile of any kind exists, treat as active (new user still setting up).
                        if (customerUser == null)
                            isActive = true;
                    }

                    cache.Set(cacheKey, isActive, TimeSpan.FromMinutes(2));
                }

                if (!isActive)
                {
                    cache.Remove(cacheKey);
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Identity/Account/Login");
                    return;
                }
            }
        }

        await _next(context);
    }
}