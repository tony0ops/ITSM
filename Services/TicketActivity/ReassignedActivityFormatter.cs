using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for ticket reassignment activities.
/// </summary>
public class ReassignedActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) => type == ActivityType.Reassigned;

    public string Format(Models.TicketActivity activity)
    {
        var from = string.IsNullOrWhiteSpace(activity.OldValue) ? "Unassigned" : activity.OldValue;
        var to = string.IsNullOrWhiteSpace(activity.NewValue) ? "Unassigned" : activity.NewValue;
        return $"Reassigned: {from} -> {to}";
    }

    public string GetIcon() => "bi-person-check";

    public string GetCssClass() => "text-info";
}