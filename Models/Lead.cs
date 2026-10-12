using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required]
    [MaxLength(50)]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Lead Source is required.")]
    [MaxLength(100)]
    public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Trade Show, Email Campaign, Social Media, Partner

    [Required(ErrorMessage = "Lead Status is required.")]
    [MaxLength(50)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required(ErrorMessage = "Priority is required.")]
    [MaxLength(50)]
    public string Priority { get; set; } = "Medium"; // High, Medium, Low

    [Required(ErrorMessage = "Expected Value is required.")]
    [Range(0, 10000000, ErrorMessage = "Expected value must be a positive number.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Expected Value ($)")]
    public decimal ExpectedValue { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public string? Notes { get; set; }

    public int? ConvertedCustomerId { get; set; }
    public int? ConvertedOpportunityId { get; set; }

    // Navigation properties
    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
