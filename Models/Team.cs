using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models;

/// <summary>
/// Team entity implementing IActivatable.
/// Teams can be deactivated when disbanded or restructured.
/// </summary>
public class Team : IActivatable
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string Description { get; set; } = "";

    public AssignmentGroup Group { get; set; }

    [Column("CreatedUtc")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // IActivatable
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<UserProfile> Members { get; set; } = new List<UserProfile>();
}