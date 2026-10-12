using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    // User info
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string FilterPeriod { get; set; } = "all"; // today, week, month, all

    // KPI Cards
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public int PendingFollowUps { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public decimal TotalWonRevenue { get; set; }
    public double WinRatePercentage { get; set; }
    public double LeadConversionRate { get; set; }

    // Chart.js Datasets (JSON serializable or lists)
    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    public List<string> PipelineStageLabels { get; set; } = new();
    public List<int> PipelineStageCounts { get; set; } = new();
    public List<decimal> PipelineStageValues { get; set; } = new();

    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlySalesValues { get; set; } = new();

    // Data lists for widgets
    public List<Activity> RecentActivities { get; set; } = new();
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<Lead> RecentLeads { get; set; } = new();
    public List<Opportunity> RecentOpportunities { get; set; } = new();
}
