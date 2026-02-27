using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

/// <summary>
/// Represents a team within the organization (e.g., "ServiceDesk L1", "Network Team").
/// Teams are linked to AssignmentGroups and contain multiple UserProfile members.
/// </summary>
public class Team : BaseEntity
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Links this team to an assignment group for ticket routing
    /// </summary>
    public AssignmentGroup Group { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<UserProfile> Members { get; set; } = new List<UserProfile>();
}