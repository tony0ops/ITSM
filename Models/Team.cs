using System.ComponentModel.DataAnnotations;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models;

/// <summary>
/// Team entity inheriting BaseEntity and implementing IActivatable.
/// Teams can be deactivated when disbanded or restructured.
/// </summary>
public class Team : BaseEntity, IActivatable
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string Description { get; set; } = "";

    public AssignmentGroup Group { get; set; }

    // IActivatable
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<UserProfile> Members { get; set; } = new List<UserProfile>();
}