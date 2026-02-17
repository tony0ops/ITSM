using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Primary Contact Name")]
    public string? PrimaryContactName { get; set; }

    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Primary Email")]
    public string? PrimaryEmail { get; set; }

    [StringLength(20)]
    [Phone]
    [Display(Name = "Primary Phone")]
    public string? PrimaryPhone { get; set; }

    // Escalation contact
    [StringLength(100)]
    [Display(Name = "Escalation Contact Name")]
    public string? EscalationContactName { get; set; }

    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Escalation Email")]
    public string? EscalationEmail { get; set; }

    [StringLength(20)]
    [Phone]
    [Display(Name = "Escalation Phone")]
    public string? EscalationPhone { get; set; }

    // Business information
    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    // Account management
    [StringLength(100)]
    [Display(Name = "Account Manager")]
    public string? AccountManager { get; set; }

    [Display(Name = "Hourly Rate")]
    [Range(0, 10000)]
    [DataType(DataType.Currency)]
    public decimal HourlyRate { get; set; } = 0;

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Status
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}