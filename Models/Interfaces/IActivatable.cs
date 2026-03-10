namespace WebApplication1.Models.Interfaces;

/// <summary>
/// Interface for entities that can be activated/deactivated.
/// Used for soft disabling of customers, teams, and users.
/// </summary>
public interface IActivatable
{
    bool IsActive { get; set; }
}