using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class ReportDashboardViewModel
{
    public string ActiveReport { get; set; } = "pipeline"; // pipeline, leads, customers, followups, activities, audit
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SelectedUser { get; set; }

    // Summary Metric Cards
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedPipeline { get; set; }
    public decimal TotalRevenueWon { get; set; }
    public int TotalCustomersCount { get; set; }
    public int TotalLeadsCount { get; set; }
    public double LeadConversionRate { get; set; }
    public int CompletedFollowUpsCount { get; set; }
    public int TotalActivitiesCount { get; set; }

    // Chart Data for Reports
    public List<string> PipelineStageNames { get; set; } = new();
    public List<decimal> PipelineStageAmounts { get; set; } = new();
    public List<string> LeadSources { get; set; } = new();
    public List<int> LeadSourceCounts { get; set; } = new();
    public List<string> OwnerNames { get; set; } = new();
    public List<decimal> OwnerPipelineAmounts { get; set; } = new();
    public List<string> ActivityTypeNames { get; set; } = new();
    public List<int> ActivityTypeCounts { get; set; } = new();

    // Table Data collections
    public List<Customer> CustomersData { get; set; } = new();
    public List<Lead> LeadsData { get; set; } = new();
    public List<Opportunity> OpportunitiesData { get; set; } = new();
    public List<FollowUp> FollowUpsData { get; set; } = new();
    public List<Activity> ActivitiesData { get; set; } = new();
    public List<AuditLog> AuditLogsData { get; set; } = new();
}
