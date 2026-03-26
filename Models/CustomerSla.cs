using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

/// <summary>
/// CustomerSla entity inheriting BaseEntity.
/// Defines customer-specific SLA response times.
/// </summary>
public class CustomerSla : BaseEntity
{
    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// Response time for Critical severity tickets (default: 1 hour)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public TimeSpan CriticalResponseTime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Response time for High severity tickets (default: 4 hours)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public TimeSpan HighResponseTime { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Response time for Medium severity tickets (default: 8 hours)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public TimeSpan MediumResponseTime { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Response time for Low severity tickets (default: 1 day)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public TimeSpan LowResponseTime { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Response time for Info severity tickets (default: 7 days)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public TimeSpan InfoResponseTime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Whether this customer SLA is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes about the SLA agreement
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
}