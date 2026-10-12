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
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public CustomersController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Customers
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, int page = 1)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSalesExec = User.IsInRole("SalesExecutive");

        var query = _context.Customers
            .Include(c => c.Opportunities)
            .AsQueryable();

        // Role-based scoping (SalesExecutive sees only own/assigned)
        if (isSalesExec && currentUser != null)
        {
            query = query.Where(c => c.OwnerId == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                     c.Email.ToLower().Contains(term) ||
                                     c.Phone.Contains(term) ||
                                     (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                                     c.CustomerCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(c => c.Status == statusFilter);
        }

        int pageSize = 8;
        int totalItems = await query.CountAsync();

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        var customersList = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerItemDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                City = c.City,
                State = c.State,
                Status = c.Status,
                OwnerName = c.OwnerId != null && users.ContainsKey(c.OwnerId) ? users[c.OwnerId] : "Unassigned",
                CreatedDate = c.CreatedDate,
                ActiveDealsCount = c.Opportunities.Count(o => o.Status == "Open"),
                TotalDealsValue = c.Opportunities.Sum(o => o.Amount)
            })
            .ToListAsync();

        var model = new CustomerListViewModel
        {
            Customers = customersList,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalCount = await _context.Customers.CountAsync(),
            ActiveCount = await _context.Customers.CountAsync(c => c.Status == "Active"),
            InactiveCount = await _context.Customers.CountAsync(c => c.Status == "Inactive")
        };

        return View(model);
    }

    // GET: Customers/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _context.Customers
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(m => m.CustomerId == id);

        if (customer == null) return NotFound();

        string ownerName = "Unassigned";
        if (!string.IsNullOrEmpty(customer.OwnerId))
        {
            var owner = await _userManager.FindByIdAsync(customer.OwnerId);
            if (owner != null) ownerName = owner.FullName;
        }

        var model = new CustomerDetailsViewModel
        {
            Customer = customer,
            OwnerName = ownerName,
            Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
            FollowUps = customer.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };

        return View(model);
    }

    // GET: Customers/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        return View(new CustomerFormViewModel());
    }

    // POST: Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        // Business Validation 1: Email uniqueness check
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
        {
            ModelState.AddModelError("Email", "A customer with this email address already exists.");
        }

        // Business Validation 2: Phone uniqueness check
        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
        {
            ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
        }

        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int nextId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000) + 1;

            var customer = new Customer
            {
                CustomerCode = $"CUST-{nextId}",
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = model.Status,
                OwnerId = model.OwnerId ?? currentUser?.Id,
                CreatedBy = currentUser?.Id,
                CreatedDate = DateTime.UtcNow,
                Notes = model.Notes
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Record in Audit Log
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Create",
                entityName: "Customer",
                recordId: customer.CustomerCode,
                newValue: $"Name: {customer.CustomerName}, Company: {customer.CompanyName}, Email: {customer.Email}",
                details: "Created new customer record successfully.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully ({customer.CustomerCode})!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateUsersDropdownAsync(model.OwnerId);
        return View(model);
    }

    // GET: Customers/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        var model = new CustomerFormViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            OwnerId = customer.OwnerId,
            Notes = customer.Notes
        };

        await PopulateUsersDropdownAsync(customer.OwnerId);
        return View(model);
    }

    // POST: Customers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
    {
        if (id != model.CustomerId) return NotFound();

        // Business Validation: Email uniqueness excluding self
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower() && c.CustomerId != id))
        {
            ModelState.AddModelError("Email", "Another customer already has this email address.");
        }

        // Business Validation: Phone uniqueness excluding self
        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone && c.CustomerId != id))
        {
            ModelState.AddModelError("Phone", "Another customer already has this phone number.");
        }

        if (ModelState.IsValid)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            string oldValue = $"Name: {customer.CustomerName}, Status: {customer.Status}, Phone: {customer.Phone}";

            customer.CustomerName = model.CustomerName.Trim();
            customer.Email = model.Email.Trim().ToLower();
            customer.Phone = model.Phone.Trim();
            customer.CompanyName = model.CompanyName?.Trim();
            customer.Address = model.Address?.Trim();
            customer.City = model.City?.Trim();
            customer.State = model.State?.Trim();
            customer.Status = model.Status;
            customer.OwnerId = model.OwnerId;
            customer.Notes = model.Notes;
            customer.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            string newValue = $"Name: {customer.CustomerName}, Status: {customer.Status}, Phone: {customer.Phone}";

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Update",
                entityName: "Customer",
                recordId: customer.CustomerCode,
                oldValue: oldValue,
                newValue: newValue,
                details: "Updated customer details.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateUsersDropdownAsync(model.OwnerId);
        return View(model);
    }

    // POST: Customers/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "User",
                action: "Delete",
                entityName: "Customer",
                recordId: customer.CustomerCode,
                oldValue: $"Name: {customer.CustomerName}, Email: {customer.Email}",
                details: $"Deleted customer {customer.CustomerName} ({customer.CustomerCode}).",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' was deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
    }
}
