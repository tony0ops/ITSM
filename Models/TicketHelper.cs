namespace WebApplication1.Models;

/// <summary>
/// Centralized display calculations for ticket list views.
/// Ensures consistent last-activity, customer-replied, and closed-state detection
/// across all ticket tables (Index, MyTickets, NewQueue, TeamQueue, portal).
/// </summary>
public static class TicketHelper
{
    /// <summary>
    /// Returns true when the ticket is in a terminal or closed-equivalent state.
    /// Use this instead of repeating the three-status check across views.
    /// </summary>
    public static bool IsClosedLike(TicketStatus status) =>
        status == TicketStatus.Closed ||
        status == TicketStatus.Resolved ||
        status == TicketStatus.Cancelled;

    /// <summary>
    /// Returns the most recent timestamp across ticket creation, updates,
    /// comments, and activities. Uses UpdatedAt when available so that
    /// edited comments and time entries also move the ticket in lists.
    /// Requires Comments and Activities to be loaded via Include().
    /// </summary>
    public static DateTime GetLastActivityAt(Ticket ticket)
    {
        var max = ticket.CreatedAt;

        if (ticket.UpdatedAt.HasValue && ticket.UpdatedAt.Value > max)
            max = ticket.UpdatedAt.Value;

        foreach (var c in ticket.Comments)
        {
            var commentTime = c.UpdatedAt.HasValue && c.UpdatedAt.Value > c.CreatedAt
                ? c.UpdatedAt.Value
                : c.CreatedAt;
            if (commentTime > max) max = commentTime;
        }

        foreach (var a in ticket.Activities)
        {
            var activityTime = a.UpdatedAt.HasValue && a.UpdatedAt.Value > a.CreatedAt
                ? a.UpdatedAt.Value
                : a.CreatedAt;
            if (activityTime > max) max = activityTime;
        }

        return max;
    }

    /// <summary>
    /// Returns true when a customer has replied and no customer-visible staff
    /// response has been posted since. Internal notes do not clear this flag
    /// because the customer cannot see them.
    /// Requires Comments to be loaded via Include().
    /// </summary>
    public static bool IsCustomerReplied(Ticket ticket)
    {
        var lastCustomerComment = ticket.Comments
            .Where(c => c.UserRole == "Customer")
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        if (lastCustomerComment == null)
            return false;

        // Only a customer-visible, non-internal staff reply clears the badge.
        var lastVisibleStaffComment = ticket.Comments
            .Where(c => c.UserRole != "Customer" && c.IsCustomerVisible && !c.IsInternal)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        return lastVisibleStaffComment == null ||
               lastCustomerComment.CreatedAt > lastVisibleStaffComment.CreatedAt;
    }

    /// <summary>
    /// Returns the Bootstrap hex color for a severity indicator dot.
    /// </summary>
    public static string GetSeverityDotColor(Severity severity) => severity switch
    {
        Severity.Critical => "#dc3545",
        Severity.High     => "#fd7e14",
        Severity.Medium   => "#0d6efd",
        Severity.Low      => "#0dcaf0",
        Severity.Info     => "#6c757d",
        _                 => "#6c757d"
    };
}