using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public ActivitiesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Activities
    public async Task<IActionResult> Index(string? typeFilter, string? statusFilter, string? searchTerm)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSalesExec = User.IsInRole("SalesExecutive");

        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .AsQueryable();

        if (isSalesExec && currentUser != null)
        {
            query = query.Where(a => a.AssignedTo == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(a => a.Subject.ToLower().Contains(term) ||
                                     (a.Description != null && a.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.Where(a => a.ActivityType == typeFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(a => a.Status == statusFilter);
        }

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        var list = await query
            .OrderByDescending(a => a.ActivityDate)
            .Select(a => new ActivityItemDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                LeadId = a.LeadId,
                RelatedType = a.CustomerId != null ? "Customer" : a.LeadId != null ? "Lead" : null,
                RelatedName = a.Customer != null ? a.Customer.CustomerName : a.Lead != null ? a.Lead.LeadName : null,
                AssignedToName = a.AssignedTo != null && users.ContainsKey(a.AssignedTo) ? users[a.AssignedTo] : "Unassigned",
                Status = a.Status
            })
            .ToListAsync();

        var all = await _context.Activities.ToListAsync();

        var model = new ActivityListViewModel
        {
            Activities = list,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
            SearchTerm = searchTerm,
            TotalActivities = all.Count,
            CallsCount = all.Count(a => a.ActivityType == "Call"),
            MeetingsCount = all.Count(a => a.ActivityType == "Meeting"),
            EmailsCount = all.Count(a => a.ActivityType == "Email"),
            TasksCount = all.Count(a => a.ActivityType == "Task")
        };

        return View(model);
    }

    // GET: Activities/Create
    public async Task<IActionResult> Create(string? type = "Call", int? customerId = null, int? leadId = null)
    {
        var model = new ActivityFormViewModel
        {
            ActivityType = type ?? "Call",
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow
        };

        await PopulateDropdownsAsync();
        return View(model);
    }

    // POST: Activities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject.Trim(),
                Description = model.Description?.Trim(),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                AssignedTo = model.AssignedTo ?? currentUser?.Id,
                Status = model.Status
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Create",
                entityName: "Activity",
                recordId: activity.ActivityId.ToString(),
                newValue: $"Type: {activity.ActivityType}, Subject: {activity.Subject}",
                details: $"Logged new {activity.ActivityType} activity.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Activity logged successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync();
        return View(model);
    }

    // POST: Activities/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var activity = await _context.Activities.FindAsync(id);
        if (activity != null)
        {
            activity.Status = activity.Status == "Completed" ? "Pending" : "Completed";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Activity marked as {activity.Status}!";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Activities/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var activity = await _context.Activities.FindAsync(id);
        if (activity != null)
        {
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Activity removed.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName");

        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName");

        var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
        ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName");
    }
}
