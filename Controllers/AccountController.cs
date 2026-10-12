using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            await _auditService.LogAsync(
                userId: model.Email,
                userName: model.Email,
                action: "Failed Login",
                entityName: "Authentication",
                result: "Failed",
                details: "Login attempt with non-existent email address.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.FullName,
                action: "Failed Login",
                entityName: "Authentication",
                result: "Blocked",
                details: "Inactive user account attempted to log in.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your system administrator.");
            return View(model);
        }

        // LockoutOnFailure: true enforces account lockout after repeated failed login attempts!
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.FullName,
                action: "Login",
                entityName: "Authentication",
                result: "Success",
                details: $"User {user.FullName} successfully authenticated into AcxiomCRM.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.FullName,
                action: "Lockout",
                entityName: "Security",
                result: "Blocked",
                details: $"User account locked out due to multiple failed login attempts. Lockout until {user.LockoutEnd}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            ModelState.AddModelError(string.Empty, "Account locked out due to multiple consecutive failed attempts. Please try again later or contact your administrator.");
            return View(model);
        }

        await _auditService.LogAsync(
            userId: user.Id,
            userName: user.FullName,
            action: "Failed Login",
            entityName: "Authentication",
            result: "Failed",
            details: "Password mismatch on login attempt.",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDemoLogin(string role)
    {
        string email = role switch
        {
            "Admin" => "admin@acxiom.com",
            "Manager" => "manager@acxiom.com",
            _ => "sales@acxiom.com"
        };

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);

            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.FullName,
                action: "Login",
                entityName: "Authentication",
                result: "Success",
                details: $"Demo quick login switch to role: {role}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Logged in as {user.FullName} ({role}).";
            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction("Login");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "An account with this email address already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Department = model.Department ?? "Sales",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            string assignedRole = string.IsNullOrWhiteSpace(model.Role) ? "SalesExecutive" : model.Role;
            await _userManager.AddToRoleAsync(user, assignedRole);

            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.FullName,
                action: "Register",
                entityName: "Authentication",
                result: "Success",
                details: $"Registered new user {user.FullName} with role {assignedRole}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = "Account registered successfully! Welcome to AcxiomCRM.";
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.FullName,
                action: "Logout",
                entityName: "Authentication",
                result: "Success",
                details: $"User {user.FullName} signed out safely.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        await _signInManager.SignOutAsync();
        TempData["SuccessMessage"] = "You have been logged out securely.";
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
