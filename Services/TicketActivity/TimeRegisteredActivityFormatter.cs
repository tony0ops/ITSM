using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for time registration activities.
/// </summary>
public class TimeRegisteredActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) => type == ActivityType.TimeRegistered;

    public string Format(Models.TicketActivity activity)
    {
        if (activity.TimeSpent.HasValue)
        {
            var hours   = activity.TimeSpent.Value.TotalHours;
            var timeStr = hours >= 1
                ? $"{hours:F1} hours"
                : $"{activity.TimeSpent.Value.TotalMinutes:F0} minutes";

            return string.IsNullOrWhiteSpace(activity.Description)
                ? $"Work logged: {timeStr}"
                : $"Work logged: {timeStr} - {activity.Description}";
        }

        return $"Work logged: {activity.Description}";
    }

    public string GetIcon() => "bi-clock-history";

    public string GetCssClass() => "text-secondary";
}