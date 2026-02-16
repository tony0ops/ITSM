namespace WebApplication1.Models;

public enum ActivityType
{
    Created,
    StatusChanged,
    Reassigned,
    TimeRegistered,
    InternalNote,
    CustomerComment,
    SeverityChanged,
    PriorityChanged,
    SlaPaused,        // NEW
    SlaResumed        // NEW
}