using System;
using WebApplication1.Data;

namespace WebApplication1.Models;

public static class SlaPolicy
{
    /// <summary>
    /// Default SLA duration by severity.
    /// </summary>
    public static TimeSpan GetSlaDuration(Severity severity)
        => severity switch
        {
            Severity.Critical => TimeSpan.FromHours(4),
            Severity.High => TimeSpan.FromHours(8),
            Severity.Medium => TimeSpan.FromHours(24),
            Severity.Low => TimeSpan.FromDays(3),
            Severity.Info => TimeSpan.FromDays(7),
            _ => TimeSpan.FromDays(3)
        };

    /// <summary>
    /// Get SLA duration with customer-specific SLA support.
    /// </summary>
    public static TimeSpan GetSlaDuration(Ticket ticket, ApplicationDbContext? context = null)
    {
        if (context != null && ticket.CustomerId.HasValue)
        {
            var customerSla = context.CustomerSlas
                .FirstOrDefault(cs => cs.CustomerId == ticket.CustomerId && cs.IsActive);

            if (customerSla != null)
            {
                return ticket.Severity switch
                {
                    Severity.Critical => customerSla.CriticalResponseTime,
                    Severity.High => customerSla.HighResponseTime,
                    Severity.Medium => customerSla.MediumResponseTime,
                    Severity.Low => customerSla.LowResponseTime,
                    Severity.Info => customerSla.InfoResponseTime,
                    _ => customerSla.MediumResponseTime
                };
            }
        }

        return GetSlaDuration(ticket.Severity);
    }

    private static readonly TimeSpan RedAbsolute = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan YellowAbsolute = TimeSpan.FromHours(5);

    private const double RedPercent = 0.10;
    private const double YellowPercent = 0.25;

    public static bool IsSlaPaused(TicketStatus status)
    {
        return status == TicketStatus.WaitingOnCustomer ||
               status == TicketStatus.WaitingOnVendor;
    }

    public static DateTime GetEffectiveDueAt(Ticket t, DateTime? now = null)
    {
        var baseDeadline = t.SlaDueAt ?? (t.CreatedAt + GetSlaDuration(t.Severity));

        var totalPause = t.TotalPausedDuration;

        if (t.SlaPausedAt.HasValue && IsSlaPaused(t.Status))
        {
            var currentPauseDuration = (now ?? DateTime.Now) - t.SlaPausedAt.Value;
            if (currentPauseDuration < TimeSpan.Zero) currentPauseDuration = TimeSpan.Zero;
            totalPause += currentPauseDuration;
        }

        return baseDeadline.Add(totalPause);
    }
    public static SlaState GetState(Ticket t, DateTime now)
    {
        if (IsSlaPaused(t.Status))
        {
            var pausedAt = t.SlaPausedAt ?? now;
            var deadlineAtPause = (t.SlaDueAt ?? t.CreatedAt + GetSlaDuration(t.Severity)) + t.TotalPausedDuration;
            return deadlineAtPause <= pausedAt ? SlaState.Breached : SlaState.Paused;
        }
        var evaluatedAt = TicketHelper.IsClosedLike(t.Status) ? t.ResolvedAt ?? t.ClosedAt ?? now : now;
        return GetState(t.CreatedAt, GetEffectiveDueAt(t, now), evaluatedAt);
    }

    public static SlaState GetState(DateTime createdAt, DateTime dueAt, DateTime now)
    {
        var remaining = dueAt - now;

        if (remaining <= TimeSpan.Zero)
            return SlaState.Breached;

        if (remaining <= RedAbsolute)
            return SlaState.Red;

        if (remaining <= YellowAbsolute)
            return SlaState.Yellow;

        var total = dueAt - createdAt;
        if (total <= TimeSpan.Zero)
            return SlaState.Red;

        var ratio = remaining.TotalSeconds / total.TotalSeconds;

        if (ratio <= RedPercent)
            return SlaState.Red;

        if (ratio <= YellowPercent)
            return SlaState.Yellow;

        return SlaState.Green;
    }

    public static string GetBadgeClass(SlaState state) => state switch
    {
        SlaState.Green => "bg-success",
        SlaState.Yellow => "bg-warning text-dark",
        SlaState.Red => "bg-danger",
        SlaState.Breached => "bg-dark",
        SlaState.Paused => "bg-info text-dark",
        _ => "bg-secondary"
    };
    public static string FormatRemaining(Ticket t, DateTime now)
    {
        if (IsSlaPaused(t.Status))
            return GetState(t, now) == SlaState.Breached ? "Paused (breached)" : "Paused";
        if (t.Status == TicketStatus.Cancelled) return "Cancelled";
        if (TicketHelper.IsClosedLike(t.Status))
            return GetState(t, now) == SlaState.Breached ? "Completed late" : "Completed within SLA";

        var dueAt = GetEffectiveDueAt(t, now);
        var remaining = dueAt - now;

        if (remaining <= TimeSpan.Zero)
            return "Breached";

        return FormatDuration(remaining);
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h";

        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";

        return $"{Math.Max(0, ts.Minutes)}m";
    }

    public static string FormatPausedDuration(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
            return "None";

        return FormatDuration(duration);
    }
}