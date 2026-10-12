using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string userId, string userRole, string userName, string period = "all");
}
