namespace WebApplication1.Models;

/// <summary>
/// Abstract base class for all domain entities.
/// Provides common audit properties: Id, CreatedAt, UpdatedAt.
/// Demonstrates inheritance and reduces property duplication across entities.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}