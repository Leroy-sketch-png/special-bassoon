using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Interfaces;
using MoePortal.Infrastructure.Data;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/admin/accounts")]
[Authorize(Policy = "AnyAdmin")]
public class AdminAccountController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEligibilityService _eligibilityService;

    public AdminAccountController(AppDbContext db, IEligibilityService eligibilityService)
    {
        _db = db;
        _eligibilityService = eligibilityService;
    }

    [HttpGet]
    public async Task<IActionResult> ListAccounts()
    {
        var records = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .Select(c => new
            {
                c.Id,
                c.Nric,
                c.FullName,
                EducationAccount = c.EducationAccount != null ? new 
                {
                    Status = c.EducationAccount.Status,
                    Balance = c.EducationAccount.Balance
                } : null,
                Age = c.AgeAsOf(DateOnly.FromDateTime(DateTime.UtcNow))
            })
            .ToListAsync();

        return Ok(records);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccountDetails(Guid id)
    {
        var record = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (record == null) return NotFound();

        var transactions = new List<EducationAccountTransaction>();
        if (record.EducationAccount != null)
        {
            var unsortedTransactions = await _db.EducationAccountTransactions
                .Where(t => t.AccountId == record.EducationAccount.Id)
                .ToListAsync();
            transactions = unsortedTransactions.OrderByDescending(t => t.TransactionDate).ToList();
        }

        return Ok(new
        {
            Record = record,
            Transactions = transactions
        });
    }

    public class OverrideRequest
    {
        public AccountStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    [HttpPost("{id}/override")]
    [Authorize(Policy = "HqAdminOnly")] // strict policy
    public async Task<IActionResult> OverrideStatus(Guid id, [FromBody] OverrideRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { Message = "Reason is required for override." });

        var updated = await _eligibilityService.AdminOverrideStatusAsync(id, req.Status, req.Reason);
        return Ok(updated);
    }

    public class CreateAccountRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    [HttpPost("{id}/create")]
    [Authorize(Policy = "HqAdminOnly")] // strict policy
    public async Task<IActionResult> CreateAccount(Guid id, [FromBody] CreateAccountRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { Message = "Reason is required for manual account creation." });

        try
        {
            var updated = await _eligibilityService.ManualCreateAccountAsync(id, req.Reason);
            return Ok(updated);
        }
        catch (MoePortal.Core.Exceptions.DomainException ex)
        {
            return BadRequest(new { ex.Code, ex.Message });
        }
    }

    public class TopUpRequest
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    [HttpPost("{id}/topup")]
    [Authorize(Policy = "AnyAdmin")]
    public async Task<IActionResult> ProposeTopUp(Guid id, [FromBody] TopUpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { Message = "Reason is required for top-up." });

        var makerId = User.FindFirstValue("sub") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "unknown_maker";
        var action = await _eligibilityService.ProposeAdminTopUpAsync(id, req.Amount, req.Reason, makerId);
        return Ok(action);
    }

    public class ApproveTopUpRequest
    {
        public bool IsApproved { get; set; }
    }

    [HttpPost("actions/{actionId}/approve")]
    [Authorize(Policy = "AnyAdmin")]
    public async Task<IActionResult> ApproveTopUp(Guid actionId, [FromBody] ApproveTopUpRequest req)
    {
        var checkerId = User.FindFirstValue("sub") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "unknown_checker";
        var record = await _eligibilityService.ApproveAdminTopUpAsync(actionId, checkerId, req.IsApproved);
        return Ok(record);
    }
}
