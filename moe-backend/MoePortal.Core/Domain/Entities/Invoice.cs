using MoePortal.Core.Domain.Enums;

namespace MoePortal.Core.Domain.Entities;

public class Invoice : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CitizenRecordId { get; set; }

    /// <summary>
    /// Total amount due. Must equal EducationAccountPortion + ExternalPspPortion.
    /// </summary>
    public decimal TotalAmount { get; set; }
    public decimal EducationAccountPortion { get; set; }
    public decimal ExternalPspPortion { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

    /// <summary>PSP-assigned session ID returned when creating payment intent.</summary>
    public string? PspPaymentSessionId { get; set; }

    /// <summary>PSP transaction reference received via webhook after capture.</summary>
    public string? PspTransactionReference { get; set; }

    /// <summary>
    /// Unique key per webhook event to prevent duplicate processing.
    /// Indexed in DB. Check before processing any webhook.
    /// </summary>
    public string? WebhookIdempotencyKey { get; set; }

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    // Navigation
    public CitizenRecord CitizenRecord { get; set; } = null!;
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}
