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
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public FollowUpsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: FollowUps
    public async Task<IActionResult> Index(string tab = "all", string? typeFilter = null, string? searchTerm = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSalesExec = User.IsInRole("SalesExecutive");

        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsQueryable();

        if (isSalesExec && currentUser != null)
        {
            query = query.Where(f => f.AssignedTo == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(f => f.Subject.ToLower().Contains(term) ||
                                     (f.Remarks != null && f.Remarks.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.Where(f => f.FollowUpType == typeFilter);
        }

        // Apply Tab filters
        switch (tab.ToLower())
        {
            case "upcoming":
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate >= DateTime.UtcNow);
                break;
            case "pending":
                query = query.Where(f => f.Status == "Planned");
                break;
            case "completed":
                query = query.Where(f => f.Status == "Completed");
                break;
            case "missed":
                query = query.Where(f => f.Status == "Missed" || (f.Status == "Planned" && f.FollowUpDate < DateTime.UtcNow));
                break;
        }

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        var list = await query
            .OrderBy(f => f.FollowUpDate)
            .Select(f => new FollowUpItemDto
            {
                FollowUpId = f.FollowUpId,
                RelatedType = f.RelatedType,
                RelatedId = f.RelatedId,
                RelatedName = f.Customer != null ? f.Customer.CustomerName :
                              f.Lead != null ? f.Lead.LeadName :
                              f.Opportunity != null ? f.Opportunity.OpportunityName : "General",
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedToName = f.AssignedTo != null && users.ContainsKey(f.AssignedTo) ? users[f.AssignedTo] : "Unassigned"
            })
            .ToListAsync();

        var allFollowUps = await _context.FollowUps.ToListAsync();

        var model = new FollowUpListViewModel
        {
            FollowUps = list,
            ActiveTab = tab,
            TypeFilter = typeFilter,
            SearchTerm = searchTerm,
            TotalCount = allFollowUps.Count,
            UpcomingCount = allFollowUps.Count(f => f.Status == "Planned" && f.FollowUpDate >= DateTime.UtcNow),
            PendingCount = allFollowUps.Count(f => f.Status == "Planned"),
            CompletedCount = allFollowUps.Count(f => f.Status == "Completed"),
            MissedCount = allFollowUps.Count(f => f.Status == "Missed" || (f.Status == "Planned" && f.FollowUpDate < DateTime.UtcNow))
        };

        return View(model);
    }

    // GET: FollowUps/Create
    public async Task<IActionResult> Create(string? relatedType = "Customer", int? relatedId = null)
    {
        var model = new FollowUpFormViewModel
        {
            RelatedType = relatedType ?? "Customer",
            RelatedId = relatedId,
            FollowUpDate = DateTime.UtcNow.AddDays(1).Date.AddHours(10)
        };

        await PopulateDropdownsAsync();
        return View(model);
    }

    // POST: FollowUps/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpFormViewModel model)
    {
        // Business Validation: Follow-up date cannot be earlier than today for a new/planned follow-up (Section 5.3 & 17.7)
        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
        }

        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var followUp = new FollowUp
            {
                RelatedType = model.RelatedType,
                RelatedId = model.RelatedId,
                CustomerId = model.RelatedType == "Customer" ? model.RelatedId : model.CustomerId,
                LeadId = model.RelatedType == "Lead" ? model.RelatedId : model.LeadId,
                OpportunityId = model.RelatedType == "Opportunity" ? model.RelatedId : model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject.Trim(),
                Remarks = model.Remarks,
                Status = model.Status,
                AssignedTo = model.AssignedTo ?? currentUser?.Id,
                CreatedDate = DateTime.UtcNow
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Create",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                newValue: $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:yyyy-MM-dd}",
                details: $"Scheduled follow-up '{followUp.Subject}'.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Follow-up scheduled successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync();
        return View(model);
    }

    // POST: FollowUps/Complete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp != null)
        {
            followUp.Status = "Completed";
            followUp.CompletedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Complete",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                newValue: "Status: Completed",
                details: $"Completed follow-up '{followUp.Subject}'.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Follow-up marked as Completed!";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: FollowUps/Reschedule/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? notes)
    {
        if (newDate.Date < DateTime.UtcNow.Date)
        {
            TempData["ErrorMessage"] = "Follow-up date cannot be earlier than today.";
            return RedirectToAction(nameof(Index));
        }

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp != null)
        {
            var oldDate = followUp.FollowUpDate;
            followUp.FollowUpDate = newDate;
            followUp.Status = "Planned";
            if (!string.IsNullOrEmpty(notes))
            {
                followUp.Remarks = (followUp.Remarks ?? "") + $" [Rescheduled from {oldDate:g}: {notes}]";
            }
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Reschedule",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: $"Date: {oldDate:g}",
                newValue: $"Date: {newDate:g}",
                details: $"Rescheduled follow-up '{followUp.Subject}' to {newDate:g}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Follow-up rescheduled to {newDate:g}!";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: FollowUps/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp != null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Delete",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                details: $"Cancelled/deleted follow-up '{followUp.Subject}'.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Follow-up removed.";
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

        var opps = await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync();
        ViewBag.OpportunitiesList = new SelectList(opps, "OpportunityId", "OpportunityName");
    }
}
