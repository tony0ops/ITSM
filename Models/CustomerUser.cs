using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models;

/// <summary>
/// Links an IdentityUser (customer portal user) to a Customer.
/// Allows multiple portal users per customer with granular permissions.
/// </summary>
public class CustomerUser : BaseEntity
{
    public int CustomerId { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Whether this user can create new tickets
    /// </summary>
    public bool CanCreateTickets { get; set; } = true;

    /// <summary>
    /// Whether this user can view tickets
    /// </summary>
    public bool CanViewTickets { get; set; } = true;

    /// <summary>
    /// Whether this user can add comments to tickets
    /// </summary>
    public bool CanAddComments { get; set; } = true;

    /// <summary>
    /// Whether this portal user is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public IdentityUser User { get; set; } = null!;
}