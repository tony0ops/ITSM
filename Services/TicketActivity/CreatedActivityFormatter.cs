using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Formatter for ticket creation activities.
/// </summary>
public class CreatedActivityFormatter : ITicketActivityFormatter
{
    public bool CanHandle(ActivityType type) => type == ActivityType.Created;

    public string Format(Models.TicketActivity activity) => activity.Description;

    public string GetIcon() => "bi-plus-circle";

    public string GetCssClass() => "text-success";
}