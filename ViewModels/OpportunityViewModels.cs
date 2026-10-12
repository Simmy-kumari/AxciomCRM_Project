using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class OpportunityListViewModel
{
    public List<OpportunityItemDto> Opportunities { get; set; } = new();
    public string ViewMode { get; set; } = "kanban"; // kanban or table
    public string? SearchTerm { get; set; }
    public string? StageFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }

    // Summary KPIs
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public decimal WonPipelineValue { get; set; }
    public decimal LostPipelineValue { get; set; }
    public int TotalDealsCount { get; set; }
    public int WonDealsCount { get; set; }
    public int OpenDealsCount { get; set; }

    // Kanban stage groups
    public List<OpportunityItemDto> QualificationDeals => Opportunities.Where(o => o.Stage == "Qualification").ToList();
    public List<OpportunityItemDto> ProposalDeals => Opportunities.Where(o => o.Stage == "Proposal").ToList();
    public List<OpportunityItemDto> NegotiationDeals => Opportunities.Where(o => o.Stage == "Negotiation").ToList();
    public List<OpportunityItemDto> WonDeals => Opportunities.Where(o => o.Stage == "Won").ToList();
    public List<OpportunityItemDto> LostDeals => Opportunities.Where(o => o.Stage == "Lost").ToList();
}

public class OpportunityItemDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CompanyName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = "Qualification";
    public int Probability { get; set; }
    public decimal WeightedAmount => (Amount * Probability) / 100m;
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = "Open";
    public string? OwnerName { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OpportunityFormViewModel : IValidatableObject
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Deal Name *")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Associated Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Associated Lead")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 100000000.00, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Deal Amount ($) *")]
    public decimal Amount { get; set; } = 10000;

    [Required(ErrorMessage = "Stage is required.")]
    [Display(Name = "Pipeline Stage *")]
    public string Stage { get; set; } = "Qualification";

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Win Probability (%) *")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date *")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Display(Name = "Status")]
    public string Status { get; set; } = "Open";

    [Display(Name = "Opportunity Owner")]
    public string? AssignedTo { get; set; }

    [Display(Name = "Lead Source / Origin")]
    public string? Source { get; set; }

    [Display(Name = "Deal Notes & Strategy")]
    public string? Notes { get; set; }

    // Enforce business validation rule: Expected Close Date cannot be in past for active opportunities
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == "Open" && ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Expected Close Date cannot be in the past for active opportunities.",
                new[] { nameof(ExpectedCloseDate) });
        }

        if (Amount <= 0)
        {
            yield return new ValidationResult(
                "Opportunity Amount must be greater than 0.",
                new[] { nameof(Amount) });
        }
    }
}

public class OpportunityDetailsViewModel
{
    public Opportunity Opportunity { get; set; } = default!;
    public string? OwnerName { get; set; }
    public List<FollowUp> FollowUps { get; set; } = new();
}
