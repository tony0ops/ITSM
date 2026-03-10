using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models;

/// <summary>
/// UserProfile entity implementing IActivatable.
/// User accounts can be deactivated when employees leave.
/// </summary>
public class UserProfile : IActivatable
{
    public int Id { get; set; }

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

    public int? TeamId { get; set; }

    [Display(Name = "Team")]
    public Team? Team { get; set; }

    [Column("CreatedUtc")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // Audit fields (exist in DB from migration)
    [StringLength(450)]
    public string CreatedBy { get; set; } = "";

    [StringLength(450)]
    public string? ModifiedBy { get; set; }

    // IActivatable
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}