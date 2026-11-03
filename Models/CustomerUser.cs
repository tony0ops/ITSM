using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models;

/// <summary>
/// Links an IdentityUser (customer portal user) to a Customer.
/// CanCreateTickets controls whether the portal user can submit support tickets.
/// </summary>
public class CustomerUser : BaseEntity
{
    public int CustomerId { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = "";

    public bool CanViewTickets   { get; set; } = true;
    public bool CanAddComments   { get; set; } = true;
    public bool CanCreateTickets { get; set; } = false;
    public bool IsActive         { get; set; } = true;

    // Navigation
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public IdentityUser User { get; set; } = null!;
}
