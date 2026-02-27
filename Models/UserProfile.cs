using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models;

/// <summary>
/// Extends ASP.NET Core Identity with ITSM-specific user attributes.
/// Maps users to teams, tracks job information, and maintains audit history.
/// </summary>
public class UserProfile : BaseEntity, IAuditable
{
    [Required, StringLength(450)]
    public string UserId { get; set; } = "";

    [ForeignKey(nameof(UserId))]
    public IdentityUser User { get; set; } = null!;

    [Required, StringLength(100)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    [StringLength(100)]
    [Display(Name = "Department")]
    public string? Department { get; set; }

    [StringLength(20)]
    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Team assignment
    public int? TeamId { get; set; }

    [Display(Name = "Team")]
    public Team? Team { get; set; }

    // IAuditable implementation
    [StringLength(450)]
    public string CreatedBy { get; set; } = "";

    [StringLength(450)]
    public string? ModifiedBy { get; set; }

    public new DateTime? UpdatedAt { get; set; }
}