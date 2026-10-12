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
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public LeadsController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Leads
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? priorityFilter, string? sourceFilter, int page = 1)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSalesExec = User.IsInRole("SalesExecutive");

        var query = _context.Leads.AsQueryable();

        if (isSalesExec && currentUser != null)
        {
            query = query.Where(l => l.AssignedTo == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                     l.Email.ToLower().Contains(term) ||
                                     l.Phone.Contains(term) ||
                                     (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                                     l.LeadCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            query = query.Where(l => l.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(priorityFilter))
        {
            query = query.Where(l => l.Priority == priorityFilter);
        }

        if (!string.IsNullOrWhiteSpace(sourceFilter))
        {
            query = query.Where(l => l.Source == sourceFilter);
        }

        int pageSize = 8;
        int totalItems = await query.CountAsync();

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        var leadsList = await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadItemDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedToName = l.AssignedTo != null && users.ContainsKey(l.AssignedTo) ? users[l.AssignedTo] : "Unassigned"
            })
            .ToListAsync();

        var allLeads = await _context.Leads.ToListAsync();

        var model = new LeadListViewModel
        {
            Leads = leadsList,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            PriorityFilter = priorityFilter,
            SourceFilter = sourceFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalCount = allLeads.Count,
            NewCount = allLeads.Count(l => l.Status == "New"),
            ContactedCount = allLeads.Count(l => l.Status == "Contacted"),
            QualifiedCount = allLeads.Count(l => l.Status == "Qualified"),
            ConvertedCount = allLeads.Count(l => l.Status == "Converted"),
            LostCount = allLeads.Count(l => l.Status == "Lost"),
            TotalExpectedPipeline = allLeads.Where(l => l.Status != "Lost" && l.Status != "Converted").Sum(l => l.ExpectedValue)
        };

        return View(model);
    }

    // GET: Leads/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _context.Leads
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .FirstOrDefaultAsync(m => m.LeadId == id);

        if (lead == null) return NotFound();

        string assignedName = "Unassigned";
        if (!string.IsNullOrEmpty(lead.AssignedTo))
        {
            var user = await _userManager.FindByIdAsync(lead.AssignedTo);
            if (user != null) assignedName = user.FullName;
        }

        Customer? convertedCust = null;
        if (lead.ConvertedCustomerId.HasValue)
        {
            convertedCust = await _context.Customers.FindAsync(lead.ConvertedCustomerId.Value);
        }

        Opportunity? convertedOpp = null;
        if (lead.ConvertedOpportunityId.HasValue)
        {
            convertedOpp = await _context.Opportunities.FindAsync(lead.ConvertedOpportunityId.Value);
        }

        var model = new LeadDetailsViewModel
        {
            Lead = lead,
            AssignedToName = assignedName,
            ConvertedCustomer = convertedCust,
            ConvertedOpportunity = convertedOpp,
            FollowUps = lead.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = lead.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };

        return View(model);
    }

    // GET: Leads/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        return View(new LeadFormViewModel());
    }

    // POST: Leads/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadFormViewModel model)
    {
        if (model.ExpectedValue < 0)
        {
            ModelState.AddModelError("ExpectedValue", "Expected value must be a positive number.");
        }

        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int nextId = (await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 2000) + 1;

            var lead = new Lead
            {
                LeadCode = $"LEAD-{nextId}",
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = model.AssignedTo ?? currentUser?.Id,
                Notes = model.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Create",
                entityName: "Lead",
                recordId: lead.LeadCode,
                newValue: $"Lead: {lead.LeadName}, Status: {lead.Status}, Value: ${lead.ExpectedValue:N0}",
                details: "Created new sales lead.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully ({lead.LeadCode})!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateUsersDropdownAsync(model.AssignedTo);
        return View(model);
    }

    // GET: Leads/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var model = new LeadFormViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes
        };

        await PopulateUsersDropdownAsync(lead.AssignedTo);
        return View(model);
    }

    // POST: Leads/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadFormViewModel model)
    {
        if (id != model.LeadId) return NotFound();

        if (ModelState.IsValid)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();

            string oldStatus = lead.Status;
            lead.LeadName = model.LeadName.Trim();
            lead.Email = model.Email.Trim().ToLower();
            lead.Phone = model.Phone.Trim();
            lead.CompanyName = model.CompanyName?.Trim();
            lead.Source = model.Source;
            lead.Status = model.Status;
            lead.Priority = model.Priority;
            lead.ExpectedValue = model.ExpectedValue;
            lead.AssignedTo = model.AssignedTo;
            lead.Notes = model.Notes;

            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Update",
                entityName: "Lead",
                recordId: lead.LeadCode,
                oldValue: $"Status: {oldStatus}",
                newValue: $"Status: {lead.Status}",
                details: $"Updated lead {lead.LeadName}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateUsersDropdownAsync(model.AssignedTo);
        return View(model);
    }

    // GET: Leads/Convert/5
    public async Task<IActionResult> Convert(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            ExpectedValue = lead.ExpectedValue,
            OpportunityName = $"{lead.CompanyName ?? lead.LeadName} - Expansion Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 25000,
            OpportunityStage = "Qualification",
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            AssignedTo = lead.AssignedTo
        };

        await PopulateUsersDropdownAsync(lead.AssignedTo);
        return View(model);
    }

    // POST: Leads/Convert/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel model)
    {
        var lead = await _context.Leads.FindAsync(model.LeadId);
        if (lead == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        Customer? createdCustomer = null;
        Opportunity? createdOpp = null;

        // 1. Create Customer if checked
        if (model.CreateCustomer)
        {
            // Check if customer already exists by email
            createdCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
            if (createdCustomer == null)
            {
                int nextCustId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000) + 1;
                createdCustomer = new Customer
                {
                    CustomerCode = $"CUST-{nextCustId}",
                    CustomerName = lead.LeadName,
                    CompanyName = lead.CompanyName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUser?.Id,
                    OwnerId = model.AssignedTo ?? currentUser?.Id,
                    Notes = $"Converted from Lead {lead.LeadCode}."
                };
                _context.Customers.Add(createdCustomer);
                await _context.SaveChangesAsync();
            }
            lead.ConvertedCustomerId = createdCustomer.CustomerId;
        }

        // 2. Create Opportunity if checked
        if (model.CreateOpportunity)
        {
            createdOpp = new Opportunity
            {
                OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.LeadName} Deal" : model.OpportunityName,
                CustomerId = createdCustomer?.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount > 0 ? model.OpportunityAmount : 10000,
                Stage = model.OpportunityStage,
                Probability = 25,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = "Open",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = model.AssignedTo ?? currentUser?.Id,
                Source = $"Converted Lead ({lead.Source})",
                Notes = $"Automatically generated from conversion of Lead {lead.LeadCode}."
            };
            _context.Opportunities.Add(createdOpp);
            await _context.SaveChangesAsync();
            lead.ConvertedOpportunityId = createdOpp.OpportunityId;
        }

        // Mark lead as Converted
        lead.Status = "Converted";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId: currentUser?.Id ?? "System",
            userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
            action: "Convert",
            entityName: "Lead",
            recordId: lead.LeadCode,
            newValue: $"Customer: {createdCustomer?.CustomerCode}, Deal: {createdOpp?.OpportunityName}",
            details: $"Successfully converted lead {lead.LeadCode} to active customer/opportunity.",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' successfully converted!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Leads/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead != null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Delete",
                entityName: "Lead",
                recordId: lead.LeadCode,
                details: $"Deleted lead record {lead.LeadName} ({lead.LeadCode}).",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' was deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
    }
}
