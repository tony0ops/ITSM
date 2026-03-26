using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;

namespace WebApplication1.Services;

/// <summary>
/// Middleware that signs out users whose UserProfile has been deactivated.
/// Uses a short-lived cache to avoid querying the database on every request.
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

                    // If no profile exists yet, treat as active
                    isActive = profile?.IsActive ?? true;

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