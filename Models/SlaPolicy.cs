using System;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Models;

public static class SlaPolicy
{
    /// <summary>
    /// Get SLA duration for a ticket, checking customer-specific SLA first, then default.
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
    /// Get SLA duration for a ticket with customer-specific SLA support.
    /// </summary>
    public static TimeSpan GetSlaDuration(Ticket ticket, ApplicationDbContext? context = null)
    {
        // Check for customer-specific SLA
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

        // Fall back to default SLA
        return GetSlaDuration(ticket.Severity);
    }

    // Absolute thresholds (these solve the "12 minutes still Green" problem)
    private static readonly TimeSpan RedAbsolute = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan YellowAbsolute = TimeSpan.FromHours(5);

    // Percentage thresholds (useful for long SLAs)
    private const double RedPercent = 0.10;     // <= 10% remaining
    private const double YellowPercent = 0.25;  // <= 25% remaining

    /// <summary>
    /// Check if ticket SLA should be paused based on status
    /// </summary>
    public static bool IsSlaPaused(TicketStatus status)
    {
        return status == TicketStatus.WaitingOnCustomer ||
               status == TicketStatus.WaitingOnVendor;
    }

    /// <summary>
    /// Calculate effective due date accounting for pauses
    /// </summary>
    public static DateTime GetEffectiveDueAt(Ticket t)
    {
        var baseDeadline = t.SlaDueAt ?? (t.CreatedAt + GetSlaDuration(t.Severity));

        // Add total paused duration
        var totalPause = t.TotalPausedDuration;

        // If currently paused, add the current pause duration
        if (t.SlaPausedAt.HasValue && IsSlaPaused(t.Status))
        {
            var currentPauseDuration = DateTime.Now - t.SlaPausedAt.Value;
            totalPause += currentPauseDuration;
        }

        return baseDeadline.Add(totalPause);
    }

    public static SlaState GetState(Ticket t)
        => GetState(t, DateTime.Now);

    public static SlaState GetState(Ticket t, DateTime now)
    {
        // If SLA is currently paused, return Green (paused state)
        if (IsSlaPaused(t.Status))
        {
            return SlaState.Green;
        }

        return GetState(t.CreatedAt, GetEffectiveDueAt(t), now);
    }

    public static SlaState GetState(DateTime createdAt, DateTime dueAt, DateTime now)
    {
        var remaining = dueAt - now;

        if (remaining <= TimeSpan.Zero)
            return SlaState.Breached;

        // 1) Absolute thresholds (always apply)
        if (remaining <= RedAbsolute)
            return SlaState.Red;

        if (remaining <= YellowAbsolute)
            return SlaState.Yellow;

        // 2) Percentage thresholds (only if total makes sense)
        var total = dueAt - createdAt;
        if (total <= TimeSpan.Zero)
            return SlaState.Red; // bad data (due <= created), treat as critical

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
        _ => "bg-secondary"
    };

    public static string FormatRemaining(Ticket t)
        => FormatRemaining(t, DateTime.Now);

    public static string FormatRemaining(Ticket t, DateTime now)
    {
        // If paused, show "Paused" instead of time
        if (IsSlaPaused(t.Status))
        {
            return "Paused";
        }

        var dueAt = GetEffectiveDueAt(t);
        var remaining = dueAt - now;

        if (remaining <= TimeSpan.Zero)
            return "Breached";

        return FormatDuration(remaining);
    }

    private static string FormatDuration(TimeSpan ts)
    {
        // Show largest 2 units (e.g., "2d 3h", "3h 14m", "12m")
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h";

        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";

        return $"{Math.Max(0, ts.Minutes)}m";
    }

    /// <summary>
    /// Format paused duration
    /// </summary>
    public static string FormatPausedDuration(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
            return "None";

        return FormatDuration(duration);
    }
}