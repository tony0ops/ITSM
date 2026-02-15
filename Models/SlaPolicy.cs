using System;
using WebApplication1.Models;

namespace WebApplication1.Models;

public static class SlaPolicy
{
    // --- v1: simple durations (local time). Adjust anytime.
    public static TimeSpan GetSlaDuration(Severity severity) => severity switch
    {
        Severity.Critical => TimeSpan.FromHours(4),
        Severity.High => TimeSpan.FromHours(8),
        Severity.Medium => TimeSpan.FromHours(24),
        Severity.Low => TimeSpan.FromDays(3),
        Severity.Info => TimeSpan.FromDays(7),
        _ => TimeSpan.FromDays(3)
    };

    // Absolute thresholds (these solve the "12 minutes still Green" problem)
    private static readonly TimeSpan RedAbsolute = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan YellowAbsolute = TimeSpan.FromHours(5);

    // Percentage thresholds (useful for long SLAs)
    private const double RedPercent = 0.10;     // <= 10% remaining
    private const double YellowPercent = 0.25;  // <= 25% remaining

    public static DateTime GetEffectiveDueAt(Ticket t)
        => t.SlaDueAt ?? (t.CreatedAt + GetSlaDuration(t.Severity));

    public static SlaState GetState(Ticket t)
        => GetState(t, DateTime.Now);

    public static SlaState GetState(Ticket t, DateTime now)
        => GetState(t.CreatedAt, GetEffectiveDueAt(t), now);

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
}