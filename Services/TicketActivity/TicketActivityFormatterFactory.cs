using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Factory for selecting the appropriate activity formatter.
/// Demonstrates polymorphism — calls different formatters based on activity type.
/// </summary>
public class TicketActivityFormatterFactory
{
    private readonly IEnumerable<ITicketActivityFormatter> _formatters;

    public TicketActivityFormatterFactory(IEnumerable<ITicketActivityFormatter> formatters)
    {
        _formatters = formatters;
    }

    /// <summary>
    /// Format an activity using the appropriate formatter.
    /// </summary>
    public string Format(Models.TicketActivity activity)
    {
        var formatter = _formatters.FirstOrDefault(f => f.CanHandle(activity.Type));
        return formatter?.Format(activity) ?? activity.Description;
    }

    /// <summary>
    /// Get icon for activity type.
    /// </summary>
    public string GetIcon(Models.TicketActivity activity)
    {
        var formatter = _formatters.FirstOrDefault(f => f.CanHandle(activity.Type));
        return formatter?.GetIcon() ?? "bi-circle";
    }

    /// <summary>
    /// Get CSS class for activity type.
    /// </summary>
    public string GetCssClass(Models.TicketActivity activity)
    {
        var formatter = _formatters.FirstOrDefault(f => f.CanHandle(activity.Type));
        return formatter?.GetCssClass() ?? "text-muted";
    }
}