using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for internal note and customer comment activities.
/// </summary>
public class CommentActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) =>
        type == ActivityType.InternalNote || type == ActivityType.CustomerComment;

    public string Format(Models.TicketActivity activity)
    {
        var prefix = activity.Type == ActivityType.InternalNote ? "Internal note" : "Customer comment";
        return $"{prefix}: {activity.Description}";
    }

    public string GetIcon() => "bi-chat-left-text";

    public string GetCssClass() => "text-info";
}