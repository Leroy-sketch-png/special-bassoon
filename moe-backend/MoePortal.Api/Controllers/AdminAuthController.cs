using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoePortal.Api.Controllers;

/// <summary>
/// Admin portal endpoints, protected by Entra ID (Microsoft Identity Web).
/// Requires a valid Entra-issued JWT with the HQ_ADMIN or SCHOOL_ADMIN role claim.
/// </summary>
[ApiController]
[Route("api/auth/admin")]
public class AdminAuthController : ControllerBase
{
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(ILogger<AdminAuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the authenticated admin's identity and role.
    /// Frontend calls this after MSAL.js acquires an Entra bearer token.
    /// Returns 403 if the Entra user has no mapped admin role.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "AnyAdmin")]
    public IActionResult AdminMe()
    {
        var oid   = User.FindFirst("oid")?.Value ?? User.FindFirst("sub")?.Value;
        var name  = User.Identity?.Name ?? User.FindFirst("name")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown";
        var email = User.FindFirst("email")?.Value ?? User.FindFirst("preferred_username")?.Value;
        var roles = User.FindAll("roles").Select(c => c.Value).ToList();
        roles.AddRange(User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value));

        // Roles must be one of: HQ_ADMIN, SCHOOL_ADMIN (configured in Entra App Registration)
        var isAdmin = roles.Any(r => r == "HQ_ADMIN" || r == "SCHOOL_ADMIN");
        if (!isAdmin)
        {
            _logger.LogWarning("Entra user {Oid} ({Email}) authenticated but has no admin role", oid, email);
            return StatusCode(403, new
            {
                message = "You do not have admin access to this portal. Please contact MOE IT to request the appropriate role."
            });
        }

        _logger.LogInformation("Admin {Oid} ({Name}) authenticated with roles: {Roles}", oid, name, string.Join(", ", roles));

        return Ok(new
        {
            UserId = oid,
            Name   = name,
            Email  = email,
            Roles  = roles
        });
    }


}
