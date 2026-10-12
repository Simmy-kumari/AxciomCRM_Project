using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string userId, string userName, string action, string entityName, 
        string? recordId = null, string? oldValue = null, string? newValue = null, 
        string? details = null, string? ipAddress = null, string result = "Success")
    {
        try
        {
            var auditEntry = new AuditLog
            {
                UserId = string.IsNullOrWhiteSpace(userId) ? "System/Anonymous" : userId,
                UserName = string.IsNullOrWhiteSpace(userName) ? "System/Guest" : userName,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                Details = details,
                IpAddress = ipAddress ?? "127.0.0.1",
                Result = result,
                CreatedDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditEntry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit log for action {Action} on entity {Entity}", action, entityName);
        }
    }
}
