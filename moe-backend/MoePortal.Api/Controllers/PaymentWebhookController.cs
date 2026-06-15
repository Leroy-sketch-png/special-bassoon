using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Interfaces;
using MoePortal.Infrastructure.Data;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/webhooks/payment")]
public class PaymentWebhookController : ControllerBase
{
    private readonly IPaymentProviderService _paymentService;
    private readonly AppDbContext _db;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(
        IPaymentProviderService paymentService,
        AppDbContext db,
        ILogger<PaymentWebhookController> logger)
    {
        _paymentService = paymentService;
        _db             = db;
        _logger         = logger;
    }

    /// <summary>
    /// Receives HitPay payment status webhooks.
    /// Protected by HMAC-SHA256 signature verification — no auth token required.
    /// Idempotent: duplicate events return 200 without reprocessing.
    /// </summary>
    [HttpPost("hitpay")]
    public async Task<IActionResult> HandleHitPayWebhook(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        // ── Parse form fields ────────────────────────────────────────────────
        var form = await Request.ReadFormAsync(ct);

        var paymentId  = form["payment_id"].FirstOrDefault() ?? string.Empty;
        var status     = form["status"].FirstOrDefault() ?? string.Empty;
        var reference  = form["reference_number"].FirstOrDefault() ?? string.Empty;
        var amountStr  = form["amount"].FirstOrDefault() ?? "0";
        var hmacHeader = form["hmac"].FirstOrDefault() ?? string.Empty;

        _logger.LogInformation(
            "HitPay webhook received: PaymentId={PaymentId} Status={Status} Reference={Reference}",
            paymentId, status, reference);

        // ── Signature verification ───────────────────────────────────────────
        var rawBodyWithoutHmac = System.Text.RegularExpressions.Regex.Replace(rawBody, @"(&?hmac=[^&]*)", "");
        if (rawBodyWithoutHmac.StartsWith("&")) rawBodyWithoutHmac = rawBodyWithoutHmac.Substring(1);

        var isValid = await _paymentService.VerifyWebhookSignatureAsync(rawBodyWithoutHmac, hmacHeader, ct);
        _logger.LogInformation("Webhook signature check: HmacHeader={Hmac}, IsValid={IsValid}", hmacHeader, isValid);
        if (!isValid)
        {
            _logger.LogWarning("HitPay webhook rejected — invalid HMAC signature. PaymentId={PaymentId}", paymentId);
            return Unauthorized(new { message = "Invalid webhook signature." });
        }

        // ── Idempotency check ────────────────────────────────────────────────
        var alreadyProcessed = await _db.Invoices
            .AnyAsync(i => i.WebhookIdempotencyKey == paymentId, ct);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Duplicate webhook event {PaymentId} — skipping.", paymentId);
            return Ok(new { message = "Already processed." });
        }

        // ── Process completed payments only ───────────────────────────────────
        if (status != "completed")
        {
            _logger.LogInformation("Ignoring non-completed webhook status: {Status}", status);
            return Ok(new { message = $"Status {status} ignored." });
        }

        if (!Guid.TryParse(reference, out var invoiceId))
        {
            _logger.LogError("Invalid reference (not a GUID): {Reference}", reference);
            return BadRequest(new { message = "Invalid reference number." });
        }

        var invoice = await _db.Invoices
            .Include(i => i.CitizenRecord)
                .ThenInclude(c => c.EducationAccount)
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null)
        {
            _logger.LogError("Invoice {InvoiceId} not found for webhook {PaymentId}", invoiceId, paymentId);
            return NotFound(new { message = "Invoice not found." });
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            _logger.LogInformation("Invoice {InvoiceId} already paid — idempotent skip.", invoiceId);
            return Ok(new { message = "Invoice already paid." });
        }

        // ── Record PSP allocation ────────────────────────────────────────────
        if (decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
                             System.Globalization.CultureInfo.InvariantCulture, out var amount) && amount > 0)
        {
            _db.PaymentAllocations.Add(new PaymentAllocation
            {
                InvoiceId = invoice.Id,
                Amount    = amount,
                Source    = "PSP",
                Reference = paymentId
            });
        }

        // ── Record Education Account allocation if applicable ─────────────────
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

            _logger.LogInformation(
                "Deducted {Amount:C} from Education Account for citizen {Nric}",
                invoice.EducationAccountPortion, invoice.CitizenRecord.Nric);
        }

        // ── Finalise invoice ──────────────────────────────────────────────────
        invoice.Status                  = InvoiceStatus.Paid;
        invoice.PspTransactionReference = paymentId;
        invoice.WebhookIdempotencyKey   = paymentId;
        invoice.PaidAt                  = DateTimeOffset.UtcNow;
        invoice.UpdatedAt               = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Invoice {InvoiceId} marked Paid via HitPay webhook {PaymentId}",
            invoiceId, paymentId);

        return Ok(new { message = "Payment processed." });
    }
}
