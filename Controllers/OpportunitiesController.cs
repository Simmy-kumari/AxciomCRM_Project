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
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public OpportunitiesController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Opportunities
    public async Task<IActionResult> Index(string? searchTerm, string? stageFilter, string? statusFilter, string view = "kanban")
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSalesExec = User.IsInRole("SalesExecutive");

        var query = _context.Opportunities
            .Include(o => o.Customer)
            .AsQueryable();

        if (isSalesExec && currentUser != null)
        {
            query = query.Where(o => o.AssignedTo == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(o => o.OpportunityName.ToLower().Contains(term) ||
                                     (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                                     (o.Customer != null && o.Customer.CompanyName != null && o.Customer.CompanyName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(stageFilter))
        {
            query = query.Where(o => o.Stage == stageFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        var list = await query
            .OrderByDescending(o => o.CreatedDate)
            .Select(o => new OpportunityItemDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                CompanyName = o.Customer != null ? o.Customer.CompanyName : null,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                OwnerName = o.AssignedTo != null && users.ContainsKey(o.AssignedTo) ? users[o.AssignedTo] : "Unassigned",
                AssignedTo = o.AssignedTo,
                CreatedDate = o.CreatedDate
            })
            .ToListAsync();

        var allDeals = await _context.Opportunities.ToListAsync();

        var model = new OpportunityListViewModel
        {
            Opportunities = list,
            ViewMode = view,
            SearchTerm = searchTerm,
            StageFilter = stageFilter,
            StatusFilter = statusFilter,
            TotalDealsCount = allDeals.Count,
            OpenDealsCount = allDeals.Count(o => o.Status == "Open"),
            WonDealsCount = allDeals.Count(o => o.Stage == "Won" || o.Status == "Won"),
            TotalPipelineValue = allDeals.Where(o => o.Status == "Open").Sum(o => o.Amount),
            WeightedPipelineValue = allDeals.Where(o => o.Status == "Open").Sum(o => (o.Amount * o.Probability) / 100m),
            WonPipelineValue = allDeals.Where(o => o.Stage == "Won" || o.Status == "Won").Sum(o => o.Amount),
            LostPipelineValue = allDeals.Where(o => o.Stage == "Lost" || o.Status == "Lost").Sum(o => o.Amount)
        };

        return View(model);
    }

    // GET: Opportunities/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var opportunity = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.FollowUps)
            .FirstOrDefaultAsync(m => m.OpportunityId == id);

        if (opportunity == null) return NotFound();

        string ownerName = "Unassigned";
        if (!string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            var user = await _userManager.FindByIdAsync(opportunity.AssignedTo);
            if (user != null) ownerName = user.FullName;
        }

        var model = new OpportunityDetailsViewModel
        {
            Opportunity = opportunity,
            OwnerName = ownerName,
            FollowUps = opportunity.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList()
        };

        return View(model);
    }

    // GET: Opportunities/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new OpportunityFormViewModel());
    }

    // POST: Opportunities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityFormViewModel model)
    {
        // Business Validation: Amount > 0
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        // Business Validation: Probability 0-100
        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        // Business Validation: Expected Close Date cannot be in the past for active opportunity
        if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
        }

        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            string status = model.Stage switch
            {
                "Won" => "Won",
                "Lost" => "Lost",
                _ => model.Status
            };

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = status,
                AssignedTo = model.AssignedTo ?? currentUser?.Id,
                Source = model.Source,
                Notes = model.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Create",
                entityName: "Opportunity",
                recordId: opp.OpportunityId.ToString(),
                newValue: $"Deal: {opp.OpportunityName}, Amount: ${opp.Amount:N0}, Stage: {opp.Stage}",
                details: "Created sales opportunity.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' added to pipeline!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(model.CustomerId, model.AssignedTo);
        return View(model);
    }

    // GET: Opportunities/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var opp = await _context.Opportunities.FindAsync(id);
        if (opp == null) return NotFound();

        var model = new OpportunityFormViewModel
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo,
            Source = opp.Source,
            Notes = opp.Notes
        };

        await PopulateDropdownsAsync(opp.CustomerId, opp.AssignedTo);
        return View(model);
    }

    // POST: Opportunities/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityFormViewModel model)
    {
        if (id != model.OpportunityId) return NotFound();

        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
        }

        if (ModelState.IsValid)
        {
            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null) return NotFound();

            string oldStage = opp.Stage;
            decimal oldAmount = opp.Amount;

            string status = model.Stage switch
            {
                "Won" => "Won",
                "Lost" => "Lost",
                _ => model.Status
            };

            opp.OpportunityName = model.OpportunityName.Trim();
            opp.CustomerId = model.CustomerId;
            opp.Amount = model.Amount;
            opp.Stage = model.Stage;
            opp.Probability = model.Probability;
            opp.ExpectedCloseDate = model.ExpectedCloseDate;
            opp.Status = status;
            opp.AssignedTo = model.AssignedTo;
            opp.Source = model.Source;
            opp.Notes = model.Notes;

            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Update",
                entityName: "Opportunity",
                recordId: opp.OpportunityId.ToString(),
                oldValue: $"Stage: {oldStage}, Amount: ${oldAmount:N0}",
                newValue: $"Stage: {opp.Stage}, Amount: ${opp.Amount:N0}",
                details: $"Updated opportunity {opp.OpportunityName}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(model.CustomerId, model.AssignedTo);
        return View(model);
    }

    // POST: Opportunities/UpdateStage (Quick Kanban Stage mover)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStage(int id, string stage)
    {
        var opp = await _context.Opportunities.FindAsync(id);
        if (opp == null) return Json(new { success = false, message = "Opportunity not found." });

        string oldStage = opp.Stage;
        opp.Stage = stage;
        if (stage == "Won")
        {
            opp.Status = "Won";
            opp.Probability = 100;
        }
        else if (stage == "Lost")
        {
            opp.Status = "Lost";
            opp.Probability = 0;
        }
        else
        {
            opp.Status = "Open";
            opp.Probability = stage switch
            {
                "Qualification" => 20,
                "Proposal" => 50,
                "Negotiation" => 80,
                _ => opp.Probability
            };
        }

        await _context.SaveChangesAsync();

        var currentUser = await _userManager.GetUserAsync(User);
        await _auditService.LogAsync(
            userId: currentUser?.Id ?? "System",
            userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
            action: "Stage Change",
            entityName: "Opportunity",
            recordId: opp.OpportunityId.ToString(),
            oldValue: $"Stage: {oldStage}",
            newValue: $"Stage: {stage}",
            details: $"Moved opportunity to {stage}.",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = $"Deal '{opp.OpportunityName}' moved to {stage}!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Opportunities/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var opp = await _context.Opportunities.FindAsync(id);
        if (opp != null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Delete",
                entityName: "Opportunity",
                recordId: opp.OpportunityId.ToString(),
                details: $"Deleted opportunity {opp.OpportunityName}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' was removed.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? selectedCustomerId = null, string? selectedUserId = null)
    {
        var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
        ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);

        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
    }
}
