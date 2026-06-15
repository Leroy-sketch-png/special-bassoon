using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Interfaces;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/edusavelifecycle")]
[Authorize(Policy = "AnyAdmin")]
public class EdusaveLifecycleController : ControllerBase
{
    private readonly IEligibilityService _eligibilityService;

    public EdusaveLifecycleController(IEligibilityService eligibilityService)
    {
        _eligibilityService = eligibilityService;
    }

    [HttpPost("evaluate/{nric}")]
    public async Task<IActionResult> Evaluate(string nric, [FromServices] Hangfire.IBackgroundJobClient backgroundJobs, [FromServices] MoePortal.Infrastructure.Data.AppDbContext db, CancellationToken ct)
    {
        var citizen = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.CitizenRecords, c => c.Nric == nric, ct);
        if (citizen == null) return NotFound("Citizen not found.");

        backgroundJobs.Enqueue<MoePortal.Api.Jobs.LifecycleJob>(j => j.ProcessCitizenLifecycleAsync(citizen.Id, CancellationToken.None));
        return Accepted(new { Message = "Lifecycle evaluation job queued successfully." });
    }

    [HttpPost("{citizenId}/override")]
    public async Task<IActionResult> OverrideStatus(Guid citizenId, [FromBody] OverrideStatusRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _eligibilityService.AdminOverrideStatusAsync(citizenId, request.NewStatus, request.Reason, ct);
            return Ok(result);
        }
        catch (MoePortal.Core.Exceptions.DomainException ex)
        {
            return BadRequest(new { ex.Code, ex.Message });
        }
    }
}

public record OverrideStatusRequest(AccountStatus NewStatus, string Reason);
