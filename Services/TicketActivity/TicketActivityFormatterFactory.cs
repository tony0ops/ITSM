using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Factory for selecting the appropriate activity formatter.
/// Calls different formatters based on activity type.
/// </summary>
public class TicketActivityFormatterFactory
{
    private readonly IEnumerable<ITicketActivityFormatter> _formatters;

    public TicketActivityFormatterFactory(IEnumerable<ITicketActivityFormatter> formatters)
    {
        _formatters = formatters;
    }

    public string Format(Models.TicketActivity activity)
    {
        var formatter = _formatters.FirstOrDefault(f => f.CanHandle(activity.Type));
        if (activity.Type == ActivityType.CommentAdded)
            return string.IsNullOrWhiteSpace(activity.NewValue) ? activity.Description : $"{activity.Description}: {activity.NewValue}";
        if (activity.Type == ActivityType.AttachmentUploaded)
            return activity.Description;
        if (activity.Type == ActivityType.TicketRestored)
            return activity.Description;
        if (activity.Type is ActivityType.CustomerChanged or ActivityType.ResolutionChanged)
            return $"{activity.Description}: {activity.OldValue ?? "None"} -> {activity.NewValue ?? "None"}";
        return formatter?.Format(activity) ?? activity.Description;
    }

    public string GetIcon(Models.TicketActivity activity)
    {
        if (activity.Type == ActivityType.CommentAdded) return "bi-chat-left-text";
        if (activity.Type == ActivityType.AttachmentUploaded) return "bi-paperclip";
        if (activity.Type == ActivityType.TicketRestored) return "bi-arrow-counterclockwise";
        var formatter = _formatters.FirstOrDefault(f => f.CanHandle(activity.Type));
        return formatter?.GetIcon() ?? "bi-circle";
    }

    public string GetCssClass(Models.TicketActivity activity)
    {
        if (activity.Type == ActivityType.CommentAdded) return activity.IsInternal ? "text-warning" : "text-primary";
        if (activity.Type == ActivityType.AttachmentUploaded) return activity.IsInternal ? "text-muted" : "text-info";
        if (activity.Type == ActivityType.TicketRestored) return "text-success";
        var formatter = _formatters.FirstOrDefault(f => f.CanHandle(activity.Type));
        return formatter?.GetCssClass() ?? "text-muted";
    }
}
