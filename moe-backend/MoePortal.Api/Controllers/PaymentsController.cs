using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Interfaces;
using MoePortal.Infrastructure.Data;
using System.Security.Claims;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(AuthenticationSchemes = "Singpass," + JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderService _psp;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        AppDbContext db,
        IPaymentProviderService psp,
        IConfiguration config,
        ILogger<PaymentsController> logger)
    {
        _db     = db;
        _psp    = psp;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Returns all invoices belonging to the authenticated citizen.
    /// </summary>
    [HttpGet("invoices")]
    public async Task<IActionResult> GetMyInvoices(CancellationToken ct)
    {
        var nric = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nric)) return Unauthorized();

        var invoices = await _db.Invoices
            .Where(i => i.CitizenRecord.Nric == nric)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.TotalAmount,
                i.EducationAccountPortion,
                i.ExternalPspPortion,
                i.Status,
                i.IssuedAt,
                i.PaidAt
            })
            .ToListAsync(ct);

        var sortedInvoices = invoices.OrderByDescending(i => i.IssuedAt).ToList();

        return Ok(sortedInvoices);
    }

    /// <summary>
    /// Returns a single invoice by ID (must belong to authenticated citizen).
    /// </summary>
    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        var nric = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nric)) return Unauthorized();

        var invoice = await _db.Invoices
            .Include(i => i.Allocations)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id && i.CitizenRecord.Nric == nric, ct);

        return invoice is null ? NotFound() : Ok(invoice);
    }

    /// <summary>
    /// Actively verifies payment status with PSP. Fallback for delayed/missing webhooks.
    /// </summary>
    [HttpPost("verify/{id:guid}")]
    public async Task<IActionResult> VerifyPayment(Guid id, CancellationToken ct)
    {
        var nric = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nric)) return Unauthorized();

        var invoice = await _db.Invoices
            .Include(i => i.CitizenRecord)
                .ThenInclude(c => c.EducationAccount)
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == id && i.CitizenRecord.Nric == nric, ct);

        if (invoice == null) return NotFound();
        if (invoice.Status == InvoiceStatus.Paid) return Ok(invoice);

        if (!string.IsNullOrEmpty(invoice.PspPaymentSessionId))
        {
            var status = await _psp.GetPaymentStatusAsync(invoice.PspPaymentSessionId, ct);
            if (status == "completed")
            {
                // Record PSP allocation
                if (invoice.ExternalPspPortion > 0)
                {
                    _db.PaymentAllocations.Add(new PaymentAllocation
                    {
                        InvoiceId = invoice.Id,
                        Amount    = invoice.ExternalPspPortion,
                        Source    = "PSP",
                        Reference = invoice.PspPaymentSessionId
                    });
                }

                // Record Education Account allocation if applicable
                if (invoice.EducationAccountPortion > 0 && invoice.CitizenRecord.EducationAccount != null)
                {
                    _db.PaymentAllocations.Add(new PaymentAllocation
                    {
                        InvoiceId = invoice.Id,
                        Amount    = invoice.EducationAccountPortion,
                        Source    = "EducationAccount",
                        Reference = "Auto-Deduct"
                    });

                    // Deduct from citizen balance
                    invoice.CitizenRecord.EducationAccount.Balance -= invoice.EducationAccountPortion;
                    
                    var transaction = new EducationAccountTransaction
                    {
                        AccountId = invoice.CitizenRecord.EducationAccount.Id,
                        Amount = -invoice.EducationAccountPortion,
                        TransactionType = "Payment",
                        Description = $"Payment for Invoice {invoice.InvoiceNumber}"
                    };
                    _db.EducationAccountTransactions.Add(transaction);
                }

                invoice.Status                  = InvoiceStatus.Paid;
                invoice.PspTransactionReference = invoice.PspPaymentSessionId;
                invoice.WebhookIdempotencyKey   = invoice.PspPaymentSessionId;
                invoice.PaidAt                  = DateTimeOffset.UtcNow;
                invoice.UpdatedAt               = DateTimeOffset.UtcNow;

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Invoice {InvoiceId} verified Paid via direct polling.", id);
            }
        }
        
        return Ok(invoice);
    }

    /// <summary>
    /// Creates a split payment intent. Deducts from Education Account first;
    /// any remainder is routed to HitPay sandbox for PayNow/card checkout.
    /// Returns a checkout URL if PSP payment is required.
    /// </summary>
    [HttpPost("intents")]
    public async Task<IActionResult> CreatePaymentIntent(
        [FromBody] CreateIntentRequest request, CancellationToken ct)
    {
        var nric = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nric)) return Unauthorized();

        var invoice = await _db.Invoices
            .Include(i => i.CitizenRecord)
                .ThenInclude(c => c.EducationAccount)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.CitizenRecord.Nric == nric, ct);

        if (invoice is null) return NotFound(new { message = "Invoice not found." });
        if (invoice.Status == InvoiceStatus.Paid)
            return Conflict(new { message = "Invoice is already paid." });

        // ── Split payment calculation ────────────────────────────────────────
        var (eaPortion, pspPortion) = SplitPaymentCalculator.CalculateSplit(
            invoice.CitizenRecord,
            invoice.TotalAmount);

        invoice.EducationAccountPortion = eaPortion;
        invoice.ExternalPspPortion      = pspPortion;

        _logger.LogInformation(
            "Payment intent for invoice {InvoiceId}: EA={EaPortion:C} PSP={PspPortion:C}",
            invoice.Id, eaPortion, pspPortion);

        // ── Call HitPay if PSP portion exists ────────────────────────────────
        string? checkoutUrl = null;
        if (pspPortion > 0)
        {
            var requestOrigin = Request.Headers["Origin"].ToString();
            if (string.IsNullOrEmpty(requestOrigin))
            {
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                {
                    requestOrigin = $"{uri.Scheme}://{uri.Authority}";
                }
            }
            var frontendOrigin = !string.IsNullOrEmpty(requestOrigin) ? requestOrigin : (_config["AllowedOrigins:Frontend"] ?? "http://localhost:3000");
            var apiBase        = _config["API_BASE_URL"] ?? $"{Request.Scheme}://{Request.Host}";

            var pspResult = await _psp.CreatePaymentSessionAsync(new CreatePaymentSessionRequest(
                InvoiceId:   invoice.Id,
                Amount:      pspPortion,
                Currency:    "SGD",
                PayerEmail:  request.PayerEmail,
                RedirectUrl: $"{frontendOrigin}/portal/payments/return?invoice={invoice.Id}",
                WebhookUrl:  $"https://example.com/api/webhooks/payment/hitpay"
            ), ct);

            if (!pspResult.Success)
            {
                _logger.LogError("HitPay session creation failed for invoice {InvoiceId}: {Error}",
                    invoice.Id, pspResult.ErrorMessage);
                return StatusCode(502, new { message = "Payment gateway error. Please try again.", detail = pspResult.ErrorMessage });
            }

            invoice.PspPaymentSessionId = pspResult.SessionId;
            checkoutUrl = pspResult.CheckoutUrl;
        }
        else
        {
            // Fully covered by Education Account
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTimeOffset.UtcNow;
            
            if (invoice.CitizenRecord.EducationAccount != null)
            {
                invoice.CitizenRecord.EducationAccount.Balance -= eaPortion;
                
                var transaction = new EducationAccountTransaction
                {
                    AccountId = invoice.CitizenRecord.EducationAccount.Id,
                    Amount = -eaPortion,
                    TransactionType = "Payment",
                    Description = $"Payment for Invoice {invoice.InvoiceNumber}"
                };
                _db.EducationAccountTransactions.Add(transaction);
            }

            var allocation = new PaymentAllocation
            {
                InvoiceId = invoice.Id,
                Source = "EducationAccount",
                Amount = eaPortion
            };
            _db.PaymentAllocations.Add(allocation);
        }

        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            InvoiceId          = invoice.Id,
            TotalAmount        = invoice.TotalAmount,
            EaPortion          = eaPortion,
            PspPortion         = pspPortion,
            CheckoutUrl        = checkoutUrl,
            RequiresPspPayment = pspPortion > 0
        });
    }

    public record CreateIntentRequest(Guid InvoiceId, string PayerEmail);
}
