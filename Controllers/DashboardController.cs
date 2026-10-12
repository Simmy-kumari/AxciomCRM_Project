using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string period = "all")
    {
        var user = await _userManager.GetUserAsync(User);
        string userId = user?.Id ?? string.Empty;
        string userName = user?.FullName ?? User.Identity?.Name ?? "User";

        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        string role = roles.FirstOrDefault() ?? "SalesExecutive";

        var model = await _dashboardService.GetDashboardDataAsync(userId, role, userName, period);
        return View(model);
    }
}
