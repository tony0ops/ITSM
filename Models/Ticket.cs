using System.ComponentModel.DataAnnotations;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models;

/// <summary>
/// Ticket entity inheriting BaseEntity and implementing ISoftDeletable.
/// Tickets are never hard-deleted due to audit and compliance requirements.
/// </summary>
public class Ticket : BaseEntity, ISoftDeletable
{
    [Required, StringLength(20)]
    public string Number { get; set; } = "";

    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000)]
    public string Description { get; set; } = "";

    public Severity Severity { get; set; } = Severity.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.New;

    public TicketCategory Category { get; set; } = TicketCategory.GeneralRequest;
    public AssignmentGroup AssignedGroup { get; set; } = AssignmentGroup.Unassigned;

    public string? AssigneeUserId { get; set; }

    [StringLength(200)]
    public string? CustomerName { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime? SlaDueAt { get; set; }

    public DateTime? SlaPausedAt { get; set; }

    public TimeSpan TotalPausedDuration { get; set; } = TimeSpan.Zero;

    [StringLength(500)]
    public string? PauseReason { get; set; }

    [StringLength(2000)]
    public string? ResolutionNotes { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    // Audit fields
    [StringLength(450)]
    public string CreatedBy { get; set; } = "";

    [StringLength(450)]
    public string? ModifiedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    [StringLength(450)]
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<TicketActivity> Activities { get; set; } = new List<TicketActivity>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}