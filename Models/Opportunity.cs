using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Opportunity
{
    [Key]
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, 100000000.00, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Deal Amount ($)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    [MaxLength(50)]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 20;

    [NotMapped]
    public decimal WeightedAmount => (Amount * Probability) / 100m;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open"; // Open, Won, Lost

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    [Display(Name = "Opportunity Owner")]
    public string? AssignedTo { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
