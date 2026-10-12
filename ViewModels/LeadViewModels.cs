using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class LeadListViewModel
{
    public List<LeadItemDto> Leads { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? PriorityFilter { get; set; }
    public string? SourceFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    // Filter counts
    public int TotalCount { get; set; }
    public int NewCount { get; set; }
    public int ContactedCount { get; set; }
    public int QualifiedCount { get; set; }
    public int ConvertedCount { get; set; }
    public int LostCount { get; set; }
    public decimal TotalExpectedPipeline { get; set; }
}

public class LeadItemDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? AssignedToName { get; set; }
}

public class LeadFormViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name *")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Business Email *")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone Number *")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Company / Organization")]
    [StringLength(150)]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Lead Source is required.")]
    [Display(Name = "Lead Source *")]
    public string Source { get; set; } = "Website";

    [Required(ErrorMessage = "Lead Status is required.")]
    [Display(Name = "Lead Status *")]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required(ErrorMessage = "Priority is required.")]
    [Display(Name = "Priority Level *")]
    public string Priority { get; set; } = "Medium"; // High, Medium, Low

    [Required(ErrorMessage = "Expected Value is required.")]
    [Range(0, 10000000, ErrorMessage = "Expected Value must be a valid positive number.")]
    [Display(Name = "Expected Value ($) *")]
    public decimal ExpectedValue { get; set; } = 10000;

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedTo { get; set; }

    [Display(Name = "Internal Notes")]
    public string? Notes { get; set; }
}

public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = default!;
    public string? AssignedToName { get; set; }
    public Customer? ConvertedCustomer { get; set; }
    public Opportunity? ConvertedOpportunity { get; set; }
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Create Customer Record")]
    public bool CreateCustomer { get; set; } = true;

    [Display(Name = "Create Sales Opportunity")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Deal Amount ($)")]
    [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than 0.")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Initial Stage")]
    public string OpportunityStage { get; set; } = "Qualification";

    [Display(Name = "Expected Close Date")]
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Display(Name = "Assign To")]
    public string? AssignedTo { get; set; }
}
