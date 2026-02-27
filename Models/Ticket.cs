using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class Ticket : BaseEntity, IAuditable, ISoftDeletable
{
    [Required, StringLength(20)]
    public string Number { get; set; } = "";

    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000)]
    public string Description { get; set; } = "";

    public Severity Severity { get; set; } = Severity.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.New;

    // Categorization
    public TicketCategory Category { get; set; } = TicketCategory.GeneralRequest;
    public AssignmentGroup AssignedGroup { get; set; } = AssignmentGroup.Unassigned;

    [Column("SlaDueUtc")]
    public DateTime? SlaDueAt { get; set; }

    // Customer relationship
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [StringLength(200)]
    public string? CustomerName { get; set; }

    public string? AssigneeUserId { get; set; }

    [Column("ClosedUtc")]
    public DateTime? ClosedAt { get; set; }

    // SLA Pause tracking
    public DateTime? SlaPausedAt { get; set; }

    [Column(TypeName = "TEXT")]
    public TimeSpan TotalPausedDuration { get; set; } = TimeSpan.Zero;

    [StringLength(500)]
    public string? PauseReason { get; set; }

    // Resolution tracking
    [StringLength(2000)]
    public string? ResolutionNotes { get; set; }

    public DateTime? ResolvedAt { get; set; }

    // IAuditable implementation
    [StringLength(450)]
    public string CreatedBy { get; set; } = "";

    [StringLength(450)]
    public string? ModifiedBy { get; set; }

    // ISoftDeletable implementation
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    [StringLength(450)]
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<TicketActivity> Activities { get; set; } = new List<TicketActivity>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}