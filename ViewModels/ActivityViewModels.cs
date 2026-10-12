using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class ActivityListViewModel
{
    public List<ActivityItemDto> Activities { get; set; } = new();
    public string? TypeFilter { get; set; } // Call, Meeting, Email, Task
    public string? StatusFilter { get; set; }
    public string? AssignedUserFilter { get; set; }
    public string? SearchTerm { get; set; }

    public int TotalActivities { get; set; }
    public int CallsCount { get; set; }
    public int MeetingsCount { get; set; }
    public int EmailsCount { get; set; }
    public int TasksCount { get; set; }
}

public class ActivityItemDto
{
    public int ActivityId { get; set; }
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public string? RelatedName { get; set; }
    public string? RelatedType { get; set; }
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public string? AssignedToName { get; set; }
    public string Status { get; set; } = "Completed";
}

public class ActivityFormViewModel
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity type is required.")]
    [Display(Name = "Activity Type *")]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    [Display(Name = "Subject *")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Description & Minutes")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    [Display(Name = "Activity Date & Time *")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Assign To")]
    public string? AssignedTo { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "Completed";
}
