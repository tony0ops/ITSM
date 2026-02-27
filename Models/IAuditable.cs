namespace WebApplication1.Models;

/// <summary>
/// Interface for entities that require audit trail tracking.
/// Tracks who created and modified entities.
/// </summary>
public interface IAuditable
{
    string CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}