using WebApplication1.Models;

namespace WebApplication1.Services.TicketActivity;

/// <summary>
/// Interface for formatting ticket activities.
/// Different implementations provide polymorphic rendering behavior.
/// </summary>
public interface ITicketActivityFormatter
{
    /// <summary>
    /// Determines if this formatter can handle the given activity type.
    /// </summary>
    bool CanHandle(ActivityType type);

    /// <summary>
    /// Format the activity for display.
    /// </summary>
    string Format(Models.TicketActivity activity);

    /// <summary>
    /// Get the Bootstrap icon class for this activity type.
    /// </summary>
    string GetIcon();

    /// <summary>
    /// Get the CSS class for styling.
    /// </summary>
    string GetCssClass();
}