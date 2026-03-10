using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for status change activities.
/// </summary>
public class StatusChangeActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) => type == ActivityType.StatusChanged;

    public string Format(Models.TicketActivity activity)
    {
        var oldStatus = string.IsNullOrWhiteSpace(activity.OldValue) ? "Unknown" : activity.OldValue;
        var newStatus = string.IsNullOrWhiteSpace(activity.NewValue) ? "Unknown" : activity.NewValue;
        return $"Status changed: {oldStatus} -> {newStatus}";
    }

    public string GetIcon() => "bi-arrow-left-right";

    public string GetCssClass() => "text-primary";
}