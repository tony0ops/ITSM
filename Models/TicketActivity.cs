using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class TicketActivity
{
    public int Id { get; set; }
    
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    
    public ActivityType Type { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    [Required, StringLength(450)]
    public string UserId { get; set; } = "";
    
    [Required, StringLength(100)]
    public string UserName { get; set; } = "";
    
    // For time registration
    public TimeSpan? TimeSpent { get; set; }
    
    // For general notes/changes
    [StringLength(4000)]
    public string Description { get; set; } = "";
    
    // For tracking changes
    [StringLength(500)]
    public string? OldValue { get; set; }
    
    [StringLength(500)]
    public string? NewValue { get; set; }
    
    public bool IsInternal { get; set; }
}
