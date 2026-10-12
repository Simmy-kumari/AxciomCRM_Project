using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Related To")]
    public string RelatedType { get; set; } = "Customer"; // Customer, Lead, Opportunity

    public int? RelatedId { get; set; }

    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public virtual Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    public virtual Opportunity? Opportunity { get; set; }

    [Required(ErrorMessage = "Follow-up date and time are required.")]
    [Display(Name = "Scheduled Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required(ErrorMessage = "Type is required.")]
    [MaxLength(50)]
    [Display(Name = "Interaction Type")]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [MaxLength(100)]
    [Display(Name = "Assigned User")]
    public string? AssignedTo { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }
}
