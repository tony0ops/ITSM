namespace WebApplication1.Models;

/// <summary>
/// Represents the type of activity recorded on a ticket.
/// </summary>
public enum ActivityType
{
    Created           = 0,
    StatusChanged     = 1,
    Reassigned        = 2,
    TimeRegistered    = 3,
    SeverityChanged   = 4,
    SlaPaused         = 5,
    SlaResumed        = 6,
    SlaDeadlineChanged = 7
}