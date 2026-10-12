using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid input payload.", errors = ModelState });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            await _auditService.LogAsync(
                userId: request.Email,
                userName: request.Email,
                action: "Failed Login",
                entityName: "API Authentication",
                result: "Failed",
                details: "API Login failure: User not found or inactive.");

            return Unauthorized(new { message = "Invalid email or credentials." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);

            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.FullName,
                action: "Login",
                entityName: "API Authentication",
                result: "Success",
                details: "Authenticated successfully via REST API.");

            return Ok(new
            {
                message = "Authentication successful.",
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    department = user.Department,
                    roles = roles
                }
            });
        }

        if (result.IsLockedOut)
        {
            return StatusCode(423, new { message = "Account is locked out due to multiple failed attempts." });
        }

        return Unauthorized(new { message = "Invalid email or credentials." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully." });
    }
}
