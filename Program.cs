using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Services.TicketActivity;

var builder = WebApplication.CreateBuilder(args);

// This is a local course app. Keep diagnostics in the console/Visual Studio output;
// the Windows Event Log provider can itself throw when the current user cannot write
// to the ".NET Runtime" source, turning an ordinary middleware warning into a crash.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddMemoryCache();
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 10 * 1024 * 1024 + 128 * 1024);
builder.Services.AddSingleton<TicketAttachmentStorage>();
builder.Services.AddScoped<SelectListService>();
builder.Services.AddScoped<EntityLifecycleService>();

// Activity formatters - one per ActivityType that needs custom rendering
builder.Services.AddScoped<ITicketActivityFormatter, CreatedActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, StatusChangeActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, ReassignedActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, TimeRegisteredActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, SeverityChangedActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, SlaPausedActivityFormatter>();
builder.Services.AddScoped<ITicketActivityFormatter, SlaDeadlineChangedActivityFormatter>();
builder.Services.AddScoped<TicketActivityFormatterFactory>();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("InternalStaff", policy =>
        policy.RequireRole("Manager", "Agent"));

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Error");

    options.Conventions.AuthorizeFolder("/Tickets",   "InternalStaff");
    options.Conventions.AuthorizeFolder("/MyTickets", "InternalStaff");
    options.Conventions.AuthorizeFolder("/Dashboard", "InternalStaff");
    options.Conventions.AuthorizeFolder("/Customers", "InternalStaff");
    options.Conventions.AuthorizeFolder("/Teams",     "InternalStaff");
    options.Conventions.AuthorizeFolder("/Users",     "InternalStaff");
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Identity/Account/Login";
    options.LogoutPath       = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Headers.CacheControl = "no-cache, no-store";
        context.Response.Headers.Pragma       = "no-cache";
        context.Response.StatusCode           = StatusCodes.Status302Found;
        context.Response.Headers.Location     = context.RedirectUri;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Headers.CacheControl = "no-cache, no-store";
        context.Response.Headers.Pragma       = "no-cache";
        context.Response.StatusCode           = StatusCodes.Status302Found;
        context.Response.Headers.Location     = context.RedirectUri;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (builder.Configuration.GetValue<bool>("LabMode:Enabled"))
        await db.Database.MigrateAsync();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = ["Manager", "Agent", "Customer"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    string[] managerEmails = builder.Configuration.GetValue<bool>("LabMode:Enabled") ? ["test1@test.com"] : [];
    foreach (var email in managerEmails)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null && !await db.CustomerUsers.IgnoreQueryFilters().AnyAsync(c => c.UserId == user.Id)
            && !await userManager.IsInRoleAsync(user, "Manager"))
            await userManager.AddToRoleAsync(user, "Manager");
    }
    // Repair missing staff profiles for previously provisioned lab accounts.
    foreach (var role in new[] { "Manager", "Agent" })
    foreach (var user in await userManager.GetUsersInRoleAsync(role))
    {
        if (!await db.UserProfiles.AnyAsync(p => p.UserId == user.Id) &&
            !await db.CustomerUsers.IgnoreQueryFilters().AnyAsync(c => c.UserId == user.Id))
        {
            db.UserProfiles.Add(new UserProfile { UserId = user.Id, DisplayName = user.Email ?? user.UserName ?? "Staff", CreatedBy = user.Id });
            await db.SaveChangesAsync();
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
app.UseMiddleware<InactiveUserMiddleware>();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
