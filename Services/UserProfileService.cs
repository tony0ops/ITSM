using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services;

/// <summary>
/// Service for managing user profiles and team assignments
/// </summary>
public class UserProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public UserProfileService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Get or create a user profile for an identity user
    /// </summary>
    public async Task<UserProfile> GetOrCreateProfileAsync(IdentityUser user)
    {
        var profile = await _context.UserProfiles
            .Include(up => up.Team)
            .FirstOrDefaultAsync(up => up.UserId == user.Id);

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = user.Id,
                DisplayName = user.UserName ?? user.Email ?? "Unknown",
                CreatedBy = "System",
                IsActive = true
            };
            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        return profile;
    }

    /// <summary>
    /// Get user profile with team information
    /// </summary>
    public async Task<UserProfile?> GetProfileWithTeamAsync(string userId)
    {
        return await _context.UserProfiles
            .Include(up => up.Team)
            .Include(up => up.User)
            .FirstOrDefaultAsync(up => up.UserId == userId);
    }

    /// <summary>
    /// Get all active team members
    /// </summary>
    public async Task<List<UserProfile>> GetTeamMembersAsync(int teamId)
    {
        return await _context.UserProfiles
            .Where(up => up.TeamId == teamId && up.IsActive)
            .OrderBy(up => up.DisplayName)
            .ToListAsync();
    }

    /// <summary>
    /// Get all teams
    /// </summary>
    public async Task<List<Team>> GetAllTeamsAsync()
    {
        return await _context.Teams
            .Include(t => t.Members)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get team by assignment group
    /// </summary>
    public async Task<Team?> GetTeamByGroupAsync(AssignmentGroup group)
    {
        return await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Group == group && t.IsActive);
    }

    /// <summary>
    /// Assign user to team
    /// </summary>
    public async Task AssignToTeamAsync(string userId, int teamId, string modifiedBy)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId);
        if (profile != null)
        {
            profile.TeamId = teamId;
            profile.ModifiedBy = modifiedBy;
            profile.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}