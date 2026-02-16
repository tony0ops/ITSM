using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Number { get; set; } = "";

    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000)]
    public string Description { get; set; } = "";

    public Severity Severity { get; set; } = Severity.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.New;

    [Column("CreatedUtc")]
    public DateTime CreatedAt { get; set; }

    [Column("SlaDueUtc")]
    public DateTime? SlaDueAt { get; set; }

    [StringLength(200)]
    public string? CustomerName { get; set; }

    public string? AssigneeUserId { get; set; }

    [Column("ClosedUtc")]
    public DateTime? ClosedAt { get; set; }

    // NEW: SLA Pause tracking
    public DateTime? SlaPausedAt { get; set; }

    [Column(TypeName = "TEXT")]
    public TimeSpan TotalPausedDuration { get; set; } = TimeSpan.Zero;

    [StringLength(500)]
    public string? PauseReason { get; set; }

    // NEW: Resolution tracking
    [StringLength(2000)]
    public string? ResolutionNotes { get; set; }

    public DateTime? ResolvedAt { get; set; }

    // Navigation property (from Phase 1)
    public ICollection<TicketActivity> Activities { get; set; } = new List<TicketActivity>();
}