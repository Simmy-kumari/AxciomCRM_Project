using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class FollowUpListViewModel
{
    public List<FollowUpItemDto> FollowUps { get; set; } = new();
    public string ActiveTab { get; set; } = "all"; // all, upcoming, pending, completed, missed
    public string? SearchTerm { get; set; }
    public string? TypeFilter { get; set; }
    public string? AssignedUserFilter { get; set; }
    public DateTime? DateFilter { get; set; }

    public int UpcomingCount { get; set; }
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int MissedCount { get; set; }
    public int TotalCount { get; set; }
}

public class FollowUpItemDto
{
    public int FollowUpId { get; set; }
    public string RelatedType { get; set; } = "Customer";
    public int? RelatedId { get; set; }
    public string? RelatedName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string FollowUpType { get; set; } = "Call";
    public string Subject { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string Status { get; set; } = "Planned";
    public string? AssignedToName { get; set; }
    public bool IsOverdue => Status == "Planned" && FollowUpDate < DateTime.UtcNow;
}

public class FollowUpFormViewModel : IValidatableObject
{
    public int FollowUpId { get; set; }

    [Required(ErrorMessage = "Related entity type is required.")]
    [Display(Name = "Relate Activity To *")]
    public string RelatedType { get; set; } = "Customer"; // Customer, Lead, Opportunity

    [Display(Name = "Related Record")]
    public int? RelatedId { get; set; }

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up date and time are required.")]
    [Display(Name = "Schedule Date & Time *")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required(ErrorMessage = "Interaction type is required.")]
    [Display(Name = "Activity Type *")]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    [Display(Name = "Subject / Objective *")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Meeting Agenda / Follow-Up Notes")]
    public string? Remarks { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [Display(Name = "Assigned Team Member")]
    public string? AssignedTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Business rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (FollowUpId == 0 && Status == "Planned" && FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Follow-up date cannot be earlier than today for a new/planned follow-up.",
                new[] { nameof(FollowUpDate) });
        }
    }
}
