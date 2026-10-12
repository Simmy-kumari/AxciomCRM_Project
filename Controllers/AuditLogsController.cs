using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? searchTerm, 
        string? moduleFilter, 
        string? actionFilter, 
        string? userFilter, 
        DateTime? fromDate, 
        DateTime? toDate, 
        int page = 1)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(a => a.UserName.ToLower().Contains(term) ||
                                     a.EntityName.ToLower().Contains(term) ||
                                     a.Action.ToLower().Contains(term) ||
                                     (a.Details != null && a.Details.ToLower().Contains(term)) ||
                                     (a.RecordId != null && a.RecordId.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(moduleFilter) && moduleFilter != "all")
        {
            query = query.Where(a => a.EntityName == moduleFilter);
        }

        if (!string.IsNullOrWhiteSpace(actionFilter) && actionFilter != "all")
        {
            query = query.Where(a => a.Action == actionFilter);
        }

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            query = query.Where(a => a.UserName == userFilter);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedDate < endOfDay);
        }

        int pageSize = 15;
        int totalItems = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var all = await _context.AuditLogs.ToListAsync();

        var model = new AuditLogListViewModel
        {
            Logs = logs,
            SearchTerm = searchTerm,
            ModuleFilter = moduleFilter,
            ActionFilter = actionFilter,
            UserFilter = userFilter,
            FromDate = fromDate,
            ToDate = toDate,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalLogsCount = all.Count,
            SecurityEventsCount = all.Count(a => a.Action == "Login" || a.Action == "Failed Login" || a.Action == "Security" || a.Action == "Role Change"),
            CrudEventsCount = all.Count(a => a.Action == "Create" || a.Action == "Update" || a.Action == "Delete")
        };

        ViewBag.ModulesList = await _context.AuditLogs.Select(a => a.EntityName).Distinct().ToListAsync();
        ViewBag.ActionsList = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();
        ViewBag.UsersList = await _context.AuditLogs.Select(a => a.UserName).Distinct().ToListAsync();

        return View(model);
    }
}
