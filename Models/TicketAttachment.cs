using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class TicketAttachment : BaseEntity
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    // Null for legacy ticket-level files; set for files submitted with a reply.
    public int? TicketCommentId { get; set; }
    public TicketComment? TicketComment { get; set; }

    [Required, StringLength(255)]
    public string FileName { get; set; } = "";

    [Required, StringLength(100)]
    public string StoredFileName { get; set; } = "";

    [Required, StringLength(150)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long FileSize { get; set; }

    [Required, StringLength(450)]
    public string UploadedByUserId { get; set; } = "";

    [Required, StringLength(100)]
    public string UploadedByName { get; set; } = "";

    [Required, StringLength(20)]
    public string UploadedByRole { get; set; } = "Agent";

    public bool IsCustomerVisible { get; set; }
}
