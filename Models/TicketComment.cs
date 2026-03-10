using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

/// <summary>
/// Represents a comment or note on a ticket.
/// Supports internal notes (hidden from customers) and customer-visible comments.
/// </summary>
public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = "";

    [Required, StringLength(100)]
    public string UserName { get; set; } = "";

    [Required, StringLength(50)]
    public string UserRole { get; set; } = "";

    [Required, StringLength(4000)]
    public string Comment { get; set; } = "";

    public bool IsInternal { get; set; } = false;
    public bool IsCustomerVisible { get; set; } = true;

    [Column("CreatedUtc")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Ticket Ticket { get; set; } = null!;
}