using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Services;
using WebApplication1.Services.TicketActivity;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<SelectListService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<EntityLifecycleService>();

// Activity formatters
builder.Services.AddScoped<ITicketActivityFormatter, CreatedActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, StatusChangeActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, ReassignedActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, TimeRegisteredActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, CommentActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, SeverityChangedActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, SlaPausedActivityFormatter>();
builder.Services.AddScoped<TicketActivityFormatterFactory>();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Require authentication for ALL pages by default
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Error");
});

// Configure login path for unauthenticated users
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Prevent browser caching of auth redirect responses
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Headers.CacheControl = "no-cache, no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.StatusCode = StatusCodes.Status302Found;
        context.Response.Headers.Location = context.RedirectUri;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Headers.CacheControl = "no-cache, no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.StatusCode = StatusCodes.Status302Found;
        context.Response.Headers.Location = context.RedirectUri;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Seed roles and assign Manager to the first registered user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = ["Manager", "Agent"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Assign Manager role to a specific user by email.
    // Change this to the email you registered with
    string[] managerEmails = ["test1@test.com"];
    foreach (var email in managerEmails)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null && !await userManager.IsInRoleAsync(user, "Manager"))
        {
            await userManager.AddToRoleAsync(user, "Manager");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Enforce inactive user sign-out on every request
app.UseMiddleware<InactiveUserMiddleware>();

app.MapRazorPages();

app.Run();
