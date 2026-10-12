using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    // GET: Users
    public async Task<IActionResult> Index(string? searchTerm, string? roleFilter, string? statusFilter)
    {
        var usersQuery = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            usersQuery = usersQuery.Where(u => u.FullName.ToLower().Contains(term) ||
                                               (u.Email != null && u.Email.ToLower().Contains(term)) ||
                                               (u.Department != null && u.Department.ToLower().Contains(term)));
        }

        if (statusFilter == "active")
        {
            usersQuery = usersQuery.Where(u => u.IsActive);
        }
        else if (statusFilter == "inactive")
        {
            usersQuery = usersQuery.Where(u => !u.IsActive);
        }

        var usersList = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
        var dtos = new List<UserItemDto>();

        foreach (var u in usersList)
        {
            var roles = await _userManager.GetRolesAsync(u);
            string role = roles.FirstOrDefault() ?? "Unassigned";

            if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "all" && !role.Equals(roleFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool isLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

            dtos.Add(new UserItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = role,
                Department = u.Department,
                IsActive = u.IsActive,
                IsLockedOut = isLocked,
                AccessFailedCount = u.AccessFailedCount,
                CreatedDate = u.CreatedDate
            });
        }

        var allUsers = await _userManager.Users.ToListAsync();

        var model = new UserListViewModel
        {
            Users = dtos,
            SearchTerm = searchTerm,
            RoleFilter = roleFilter,
            StatusFilter = statusFilter,
            TotalUsers = allUsers.Count,
            ActiveUsers = allUsers.Count(u => u.IsActive),
            LockedUsers = allUsers.Count(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow)
        };

        return View(model);
    }

    // GET: Users/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateRolesDropdownAsync();
        return View(new UserCreateViewModel());
    }

    // POST: Users/Create
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = true,
                FullName = model.FullName.Trim(),
                Department = model.Department?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(
                    userId: currentUser?.Id ?? "System",
                    userName: currentUser?.FullName ?? User.Identity?.Name ?? "Admin",
                    action: "Create",
                    entityName: "User",
                    recordId: user.Id,
                    newValue: $"User: {user.FullName}, Role: {model.Role}",
                    details: $"Administrator created user account for {user.FullName}.",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = $"User '{user.FullName}' registered successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        await PopulateRolesDropdownAsync(model.Role);
        return View(model);
    }

    // GET: Users/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        string currentRole = roles.FirstOrDefault() ?? "SalesExecutive";

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Department = user.Department,
            Role = currentRole,
            IsActive = user.IsActive
        };

        await PopulateRolesDropdownAsync(currentRole);
        return View(model);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            string oldRole = currentRoles.FirstOrDefault() ?? "None";

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();
            user.Department = model.Department?.Trim();
            user.IsActive = model.IsActive;

            // Optional password reset
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!resetResult.Succeeded)
                {
                    foreach (var err in resetResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    await PopulateRolesDropdownAsync(model.Role);
                    return View(model);
                }
            }

            // Role update
            if (oldRole != model.Role)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            await _userManager.UpdateAsync(user);

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "Admin",
                action: "Role Change",
                entityName: "User",
                recordId: user.Id,
                oldValue: $"Role: {oldRole}",
                newValue: $"Role: {model.Role}, Active: {model.IsActive}",
                details: $"Updated user profile and credentials for {user.FullName}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateRolesDropdownAsync(model.Role);
        return View(model);
    }

    // POST: Users/ToggleStatus/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "Admin",
                action: "Security",
                entityName: "User",
                recordId: user.Id,
                newValue: $"IsActive: {user.IsActive}",
                details: $"Toggled user account active state to {user.IsActive}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Account status for {user.FullName} changed to {(user.IsActive ? "Active" : "Inactive")}.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Users/Unlock/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            var currentUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentUser?.Id ?? "System",
                userName: currentUser?.FullName ?? User.Identity?.Name ?? "Admin",
                action: "Security",
                entityName: "User",
                recordId: user.Id,
                newValue: "Lockout: Reset",
                details: $"Admin unlocked account for {user.FullName}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Account for {user.FullName} has been unlocked successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateRolesDropdownAsync(string? selectedRole = null)
    {
        var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        ViewBag.RolesList = new SelectList(roles, selectedRole);
    }
}
