using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string userId, string userName, string action, string entityName, 
        string? recordId = null, string? oldValue = null, string? newValue = null, 
        string? details = null, string? ipAddress = null, string result = "Success");
}
