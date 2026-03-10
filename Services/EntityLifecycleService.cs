using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Services;

/// <summary>
/// Service for managing entity lifecycle operations.
/// Provides polymorphic operations on entities implementing IActivatable or ISoftDeletable.
/// </summary>
public class EntityLifecycleService
{
    private readonly ApplicationDbContext _context;

    public EntityLifecycleService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Deactivate any entity implementing IActivatable.
    /// Works with Customer, Team, UserProfile, etc.
    /// </summary>
    public async Task<bool> DeactivateAsync<T>(int id) where T : class, IActivatable
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity == null) return false;

        entity.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Reactivate any entity implementing IActivatable.
    /// </summary>
    public async Task<bool> ActivateAsync<T>(int id) where T : class, IActivatable
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity == null) return false;

        entity.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Soft delete any entity implementing ISoftDeletable.
    /// Record is marked as deleted but preserved in database for audit.
    /// </summary>
    public async Task<bool> SoftDeleteAsync<T>(int id, string deletedBy) where T : class, ISoftDeletable
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = deletedBy;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Restore a soft-deleted entity. Uses IgnoreQueryFilters to find filtered-out records.
    /// </summary>
    public async Task<bool> RestoreAsync<T>(int id) where T : class, ISoftDeletable
    {
        var entity = await _context.Set<T>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        if (entity == null) return false;

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get all active entities implementing IActivatable.
    /// </summary>
    public IQueryable<T> GetActive<T>() where T : class, IActivatable
    {
        return _context.Set<T>().Where(e => e.IsActive);
    }

    /// <summary>
    /// Get all non-deleted entities implementing ISoftDeletable.
    /// </summary>
    public IQueryable<T> GetNonDeleted<T>() where T : class, ISoftDeletable
    {
        return _context.Set<T>().Where(e => !e.IsDeleted);
    }
}