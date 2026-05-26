using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for SLA deadline change activities.
/// </summary>
public class SlaDeadlineChangedActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) => type == ActivityType.SlaDeadlineChanged;

    public string Format(Models.TicketActivity activity)
    {
        var oldVal = string.IsNullOrWhiteSpace(activity.OldValue) ? "Auto" : activity.OldValue;
        var newVal = string.IsNullOrWhiteSpace(activity.NewValue) ? "None" : activity.NewValue;
        return $"SLA deadline changed: {oldVal} -> {newVal}";
    }

    public string GetIcon() => "bi-calendar-event";

    public string GetCssClass() => "text-info";
}