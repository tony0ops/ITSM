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

    // Neutral naming in class (still stored in existing DB columns)
    [Column("CreatedUtc")]
    public DateTime CreatedAt { get; set; }

    [Column("SlaDueUtc")]
    public DateTime? SlaDueAt { get; set; }

    [StringLength(200)]
    public string? CustomerName { get; set; }

    public string? AssigneeUserId { get; set; }

    [Column("ClosedUtc")]
    public DateTime? ClosedAt { get; set; }
}