using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services;

/// <summary>
/// Service for generating common SelectLists used across the application
/// </summary>
public class SelectListService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SelectListService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Get all active customers sorted by company name
    /// </summary>
    public SelectList GetActiveCustomers()
    {
        return new SelectList(
            _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .AsNoTracking()
                .Select(c => new { c.Id, c.CompanyName }),
            "Id",
            "CompanyName"
        );
    }

    /// <summary>
    /// Get all application users sorted by email
    /// </summary>
    public async Task<SelectList> GetUsersAsync()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .AsNoTracking()
            .ToListAsync();

        return new SelectList(
            users.Select(u => new
            {
                Id = u.Id,
                Name = u.Email ?? u.UserName ?? "Unknown"
            }),
            "Id",
            "Name"
        );
    }

    /// <summary>
    /// Get all customers (including inactive) sorted by company name
    /// </summary>
    public SelectList GetAllCustomers()
    {
        return new SelectList(
            _context.Customers
                .OrderBy(c => c.CompanyName)
                .AsNoTracking()
                .Select(c => new { c.Id, c.CompanyName }),
            "Id",
            "CompanyName"
        );
    }
}
