using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for severity change activities.
/// </summary>
public class SeverityChangedActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) =>
        type == ActivityType.SeverityChanged || type == ActivityType.PriorityChanged;

    public string Format(Models.TicketActivity activity)
    {
        var oldVal = string.IsNullOrWhiteSpace(activity.OldValue) ? "Unknown" : activity.OldValue;
        var newVal = string.IsNullOrWhiteSpace(activity.NewValue) ? "Unknown" : activity.NewValue;
        return $"Severity changed: {oldVal} → {newVal}";
    }

    public string GetIcon() => "bi-exclamation-triangle";

    public string GetCssClass() => "text-danger";
}