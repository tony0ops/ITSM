using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for SLA pause/resume activities.
/// </summary>
public class SlaPausedActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) =>
        type == ActivityType.SlaPaused || type == ActivityType.SlaResumed;

    public string Format(Models.TicketActivity activity)
    {
        return activity.Type == ActivityType.SlaPaused
            ? $"SLA paused: {activity.Description}"
            : $"SLA resumed: {activity.Description}";
    }

    public string GetIcon() => "bi-pause-circle";

    public string GetCssClass() => "text-warning";
}