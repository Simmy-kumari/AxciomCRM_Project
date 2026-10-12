using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity type is required.")]
    [MaxLength(50)]
    [Display(Name = "Activity Type")]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity date is required.")]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public virtual Lead? Lead { get; set; }

    [MaxLength(100)]
    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Completed"; // Completed, Pending
}
