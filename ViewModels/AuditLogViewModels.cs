using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class AuditLogListViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? ModuleFilter { get; set; }
    public string? ActionFilter { get; set; }
    public string? UserFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    // Filter counts
    public int TotalLogsCount { get; set; }
    public int SecurityEventsCount { get; set; }
    public int CrudEventsCount { get; set; }
}
