namespace MoePortal.Core.Domain.Entities;

public class PaymentAllocation : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string Source { get; set; } = string.Empty; // "EducationAccount" | "PSP"
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset AllocatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
