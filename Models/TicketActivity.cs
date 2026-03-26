using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

/// <summary>
/// TicketActivity entity inheriting BaseEntity.
/// Tracks all changes, time entries, and status transitions on a ticket.
/// </summary>
public class TicketActivity : BaseEntity
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public ActivityType Type { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = "";

    [Required, StringLength(100)]
    public string UserName { get; set; } = "";

    public TimeSpan? TimeSpent { get; set; }

    [StringLength(4000)]
    public string Description { get; set; } = "";

    [StringLength(500)]
    public string? OldValue { get; set; }

    [StringLength(500)]
    public string? NewValue { get; set; }

    public bool IsInternal { get; set; }
}
