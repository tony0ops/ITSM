namespace WebApplication1.Models.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// Records are marked as deleted instead of being physically removed.
/// This preserves historical data and supports audit requirements.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}