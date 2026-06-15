using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain.Entities;
using MoePortal.Infrastructure.Data;
using System.Security.Claims;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/fas")]
public class FasController : ControllerBase
{
    private readonly AppDbContext _db;

    public FasController(AppDbContext db)
    {
        _db = db;
    }

    public class SubmitApplicationRequest
    {
        public string ApplicationDataJson { get; set; } = string.Empty;
    }

    [HttpPost("submit")]
    [Authorize(AuthenticationSchemes = "Singpass," + JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> SubmitApplication([FromBody] SubmitApplicationRequest req)
    {
        var nric = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nric)) return Unauthorized();

        // Reject any attempt to auto-set consent or declarations via the API
        if (req.ApplicationDataJson.Contains("\"consent_given\"") || req.ApplicationDataJson.Contains("\"declaration_true\""))
        {
            return BadRequest("Consent and declaration fields cannot be auto-populated. They must be explicitly provided by the user.");
        }

        var citizen = await _db.CitizenRecords.FirstOrDefaultAsync(c => c.Nric == nric);
        if (citizen == null) return NotFound("Citizen not found");

        var app = new FasApplication
        {
            CitizenRecordId = citizen.Id,
            ApplicationDataJson = req.ApplicationDataJson
        };

        _db.FasApplications.Add(app);
        
        // Also clear any drafts they had
        var draft = await _db.FasApplicationDrafts.FirstOrDefaultAsync(d => d.CitizenRecordId == citizen.Id);
        if (draft != null)
        {
            _db.FasApplicationDrafts.Remove(draft);
        }

        await _db.SaveChangesAsync();

        return Ok(app);
    }

    [HttpGet("admin/list")]
    [Authorize(Policy = "AnyAdmin")]
    public async Task<IActionResult> ListApplications()
    {
        var appsUnsorted = await _db.FasApplications
            .Include(a => a.CitizenRecord)
            .Select(a => new {
                a.Id,
                a.Status,
                a.SubmittedAt,
                a.ReviewedAt,
                CitizenName = a.CitizenRecord!.FullName,
                CitizenNric = a.CitizenRecord!.Nric,
                a.ApplicationDataJson
            })
            .ToListAsync();
            
        var apps = appsUnsorted.OrderByDescending(a => a.SubmittedAt).ToList();

        var result = apps.Select(a => {
            var isEligible = false;
            try {
                using var doc = System.Text.Json.JsonDocument.Parse(a.ApplicationDataJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("household_income", out var incProp) && 
                    root.TryGetProperty("household_size", out var sizeProp))
                {
                    var income = incProp.GetDecimal();
                    var size = sizeProp.GetDecimal();
                    var pci = size > 0 ? income / size : 0;
                    isEligible = income <= 3000 || pci <= 750;
                }
            } catch { /* Ignore parse errors */ }

            return new {
                a.Id,
                a.Status,
                a.SubmittedAt,
                a.ReviewedAt,
                a.CitizenName,
                a.CitizenNric,
                PreliminaryTier = isEligible ? "Eligible" : "Not Eligible"
            };
        });

        return Ok(result);
    }

    [HttpGet("admin/{id}")]
    [Authorize(Policy = "AnyAdmin")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var app = await _db.FasApplications
            .Include(a => a.CitizenRecord)
            .FirstOrDefaultAsync(a => a.Id == id);
            
        if (app == null) return NotFound();
        return Ok(app);
    }

    public class ReviewRequest
    {
        public MoePortal.Core.Domain.Enums.FasApplicationStatus Status { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    [HttpPost("admin/{id}/review")]
    [Authorize(Policy = "AnyAdmin")]
    public async Task<IActionResult> ReviewApplication(Guid id, [FromBody] ReviewRequest req)
    {
        var app = await _db.FasApplications.Include(a => a.CitizenRecord).FirstOrDefaultAsync(a => a.Id == id);
        if (app == null) return NotFound();

        // Maker workflow
        app.Status = MoePortal.Core.Domain.Enums.FasApplicationStatus.PendingApproval; // Force into Maker/Checker flow
        app.AdminRemarks = req.Remarks;
        app.MakerId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown_maker";
        app.ReviewedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(app);
    }

    [HttpPost("admin/{id}/approve")]
    [Authorize(Policy = "AnyAdmin")]
    public async Task<IActionResult> ApproveApplication(Guid id, [FromBody] ReviewRequest req, [FromServices] Hangfire.IBackgroundJobClient backgroundJobs, [FromServices] MoePortal.Core.Interfaces.IEligibilityService eligibilityService)
    {
        var app = await _db.FasApplications.Include(a => a.CitizenRecord).FirstOrDefaultAsync(a => a.Id == id);
        if (app == null) return NotFound();

        if (app.Status != MoePortal.Core.Domain.Enums.FasApplicationStatus.PendingApproval)
        {
            return BadRequest("Application must be in PendingApproval state.");
        }

        var checkerId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown_checker";

        if (req.Status != MoePortal.Core.Domain.Enums.FasApplicationStatus.Approved && req.Status != MoePortal.Core.Domain.Enums.FasApplicationStatus.Rejected)
        {
            return BadRequest("Invalid status. Must be 'Approved' or 'Rejected'.");
        }

        app.Status = req.Status; // Can be Approved or Rejected
        app.CheckerId = checkerId;
        app.AdminRemarks = string.IsNullOrWhiteSpace(req.Remarks) ? app.AdminRemarks : app.AdminRemarks + "\n[Checker]: " + req.Remarks;

        if (req.Status == MoePortal.Core.Domain.Enums.FasApplicationStatus.Approved && app.CitizenRecord != null)
        {
            // Hangfire async job queueing
            backgroundJobs.Enqueue<MoePortal.Api.Jobs.LifecycleJob>(j => j.ProcessCitizenLifecycleAsync(app.CitizenRecord.Id, CancellationToken.None));
            await eligibilityService.SystemTopUpAsync(app.CitizenRecord.Id, 200.00m, "FAS Grant - Approved");
        }

        await _db.SaveChangesAsync();
        return Ok(app);
    }
}
