using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

/// <summary>
/// Represents a comment or note on a ticket.
/// Separate from TicketActivity - focused on communication rather than tracking changes.
/// Supports internal notes (hidden from customers) and customer-visible comments.
/// </summary>
public class TicketComment : BaseEntity
{
    public int TicketId { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = "";

    [Required, StringLength(100)]
    public string UserName { get; set; } = "";

    [Required, StringLength(50)]
    public string UserRole { get; set; } = ""; // "Agent", "Customer", "Admin"

    [Required, StringLength(4000)]
    public string Comment { get; set; } = "";

    /// <summary>
    /// Internal note - hidden from customers
    /// </summary>
    public bool IsInternal { get; set; } = false;

    /// <summary>
    /// Whether this comment is visible to customers (false if internal)
    /// </summary>
    public bool IsCustomerVisible { get; set; } = true;

    // Navigation
    public Ticket Ticket { get; set; } = null!;
}