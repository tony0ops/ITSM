namespace WebApplication1.Models;

/// <summary>
/// Applies the ticket lifecycle changes caused by a public reply.
/// </summary>
public static class TicketConversationFlow
{
    public static bool CanPostPublicReply(TicketStatus status) =>
        status is not (TicketStatus.Closed or TicketStatus.Cancelled);

    public static IEnumerable<TicketActivity> ApplyPublicReply(
        Ticket ticket, string userId, string userName, DateTime now)
    {
        var oldStatus = ticket.Status;
        if (oldStatus is not (TicketStatus.WaitingOnCustomer or TicketStatus.Resolved))
            return [];

        var activities = new List<TicketActivity>();
        if (oldStatus == TicketStatus.WaitingOnCustomer)
        {
            if (ticket.SlaPausedAt.HasValue)
            {
                var pauseDuration = now - ticket.SlaPausedAt.Value;
                if (pauseDuration < TimeSpan.Zero) pauseDuration = TimeSpan.Zero;
                ticket.TotalPausedDuration += pauseDuration;
                activities.Add(new TicketActivity
                {
                    TicketId = ticket.Id,
                    Type = ActivityType.SlaResumed,
                    CreatedAt = now,
                    UserId = userId,
                    UserName = userName,
                    Description = $"SLA resumed (was paused for {SlaPolicy.FormatPausedDuration(pauseDuration)})",
                    OldValue = oldStatus.ToString(),
                    NewValue = TicketStatus.InProgress.ToString(),
                    IsInternal = true
                });
            }

            ticket.SlaPausedAt = null;
            ticket.PauseReason = null;
        }

        ticket.Status = TicketStatus.InProgress;
        ticket.ResolutionNotes = null;
        ticket.ResolvedAt = null;
        ticket.ClosedAt = null;
        activities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            Type = ActivityType.StatusChanged,
            CreatedAt = now,
            UserId = userId,
            UserName = userName,
            Description = "Ticket reopened by public reply",
            OldValue = oldStatus.ToString(),
            NewValue = ticket.Status.ToString(),
            IsInternal = false
        });

        return activities;
    }
}
