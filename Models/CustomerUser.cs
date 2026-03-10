using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models;

/// <summary>
/// Links an IdentityUser (customer portal user) to a Customer.
/// Allows multiple portal users per customer with granular permissions.
/// </summary>
public class CustomerUser
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = "";

    public bool CanCreateTickets { get; set; } = true;
    public bool CanViewTickets { get; set; } = true;
    public bool CanAddComments { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [Column("CreatedUtc")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public IdentityUser User { get; set; } = null!;
}