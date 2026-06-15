using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoePortal.Infrastructure.Data;
using System.Security.Claims;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/eligibility")]
[Authorize(AuthenticationSchemes = "Singpass," + JwtBearerDefaults.AuthenticationScheme)]
public class EligibilityController : ControllerBase
{
    private readonly AppDbContext _db;

    public EligibilityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var nric = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nric)) return Unauthorized();

        var citizen = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .Include(c => c.Invoices.Where(i => i.Status == Core.Domain.Enums.InvoiceStatus.Pending))
                .ThenInclude(i => i.LineItems)
            .Include(c => c.Invoices)
                .ThenInclude(i => i.Allocations)
            .FirstOrDefaultAsync(c => c.Nric == nric, ct);

        if (citizen == null) return NotFound("Citizen record not found.");

        return Ok(citizen);
    }
    [HttpGet("me/transactions")]
    public async Task<IActionResult> GetMyTransactions(CancellationToken ct)
    {
        var nric = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nric)) return Unauthorized();

        var citizen = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .FirstOrDefaultAsync(c => c.Nric == nric, ct);
            
        if (citizen == null || citizen.EducationAccount == null) return NotFound("Citizen or Account not found.");

        var transactions = await _db.EducationAccountTransactions
            .Where(t => t.AccountId == citizen.EducationAccount.Id)
            .ToListAsync(ct);

        var sortedTransactions = transactions.OrderByDescending(t => t.TransactionDate).ToList();

        return Ok(sortedTransactions);
    }
}
