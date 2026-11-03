using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services;

/// <summary>
/// Service for generating common SelectLists used across the application.
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
    /// Get all active customers sorted by company name.
    /// </summary>
    public SelectList GetActiveCustomers(int? includeCustomerId = null)
    {
        return new SelectList(
            _context.Customers
                .Where(c => c.IsActive || c.Id == includeCustomerId)
                .OrderBy(c => c.CompanyName)
                .AsNoTracking()
                .Select(c => new { c.Id, CompanyName = c.CompanyName + (c.IsActive ? "" : " (inactive)") }),
            "Id",
            "CompanyName"
        );
    }

    /// <summary>
    /// Get all active internal staff users sorted by email.
    /// </summary>
    public async Task<SelectList> GetUsersAsync()
    {
        var staffProfiles = await _context.UserProfiles
            .Include(up => up.User)
            .Where(up => up.IsActive && _context.UserRoles.Any(ur => ur.UserId == up.UserId &&
                _context.Roles.Any(r => r.Id == ur.RoleId && (r.Name == "Manager" || r.Name == "Agent"))))
            .OrderBy(up => up.User.Email)
            .AsNoTracking()
            .ToListAsync();

        return new SelectList(
            staffProfiles.Select(up => new
            {
                Id = up.UserId,
                Name = up.User?.Email ?? up.DisplayName
            }),
            "Id",
            "Name"
        );
    }

    /// <summary>
    /// Get active staff from all active teams mapped to an assignment group.
    /// - Unassigned: returns all active staff (used for unfiltered dropdowns).
    /// - Named group with an active team and members: returns only those members.
    /// - Named group with no active team or no members: returns an empty list.
    ///   The caller's UI should indicate no assignees are available for that group.
    /// </summary>
    public async Task<SelectList> GetUsersByGroupAsync(AssignmentGroup group)
    {
        if (group == AssignmentGroup.Unassigned)
            return await GetUsersAsync();

        // Teams sharing a group share its queue and all active staff are eligible.
        var members = await _context.UserProfiles.Include(p => p.User)
            .Where(p => p.IsActive && p.Team != null && p.Team.IsActive && p.Team.Group == group && _context.UserRoles.Any(ur => ur.UserId == p.UserId &&
                _context.Roles.Any(r => r.Id == ur.RoleId && (r.Name == "Manager" || r.Name == "Agent"))))
            .OrderBy(p => p.User.Email)
            .Select(p => new { Id = p.UserId, Name = p.User.Email ?? p.DisplayName })
            .ToListAsync();
        return new SelectList(members, "Id", "Name");
    }
}
