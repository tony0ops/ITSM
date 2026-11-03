using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

/// <summary>
/// TicketComment entity inheriting BaseEntity.
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
    public string UserRole { get; set; } = "";

    [Required, StringLength(4000)]
    public string Comment { get; set; } = "";

    public bool IsInternal { get; set; } = false;
    public bool IsCustomerVisible { get; set; } = true;

    // Navigation
    public Ticket Ticket { get; set; } = null!;
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
