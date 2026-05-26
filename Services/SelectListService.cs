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
    /// Get all active internal staff users sorted by email.
    /// </summary>
    public async Task<SelectList> GetUsersAsync()
    {
        var staffProfiles = await _context.UserProfiles
            .Include(up => up.User)
            .Where(up => up.IsActive)
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
    /// Get users belonging to the team mapped to a specific assignment group.
    /// - Unassigned: returns all active staff (used for unfiltered dropdowns).
    /// - Named group with an active team and members: returns only those members.
    /// - Named group with no active team or no members: returns an empty list.
    ///   The caller's UI should indicate no assignees are available for that group.
    /// </summary>
    public async Task<SelectList> GetUsersByGroupAsync(AssignmentGroup group)
    {
        if (group == AssignmentGroup.Unassigned)
            return await GetUsersAsync();

        var team = await _context.Teams
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Group == group && t.IsActive);

        if (team != null)
        {
            var members = team.Members
                .Where(m => m.IsActive && m.User != null)
                .OrderBy(m => m.User.Email)
                .Select(m => new
                {
                    Id = m.UserId,
                    Name = m.User.Email ?? m.DisplayName
                })
                .ToList();

            if (members.Count > 0)
                return new SelectList(members, "Id", "Name");
        }

        // No active team or no members mapped to this group.
        // Return an empty list - the assignee dropdown will be empty,
        // and the Unassigned+assignee validation will prevent an inconsistent save.
        return new SelectList(Enumerable.Empty<object>(), "Id", "Name");
    }
}