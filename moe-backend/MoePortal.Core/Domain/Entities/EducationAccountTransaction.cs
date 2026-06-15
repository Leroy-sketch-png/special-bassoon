using System;

namespace MoePortal.Core.Domain.Entities;

public class EducationAccountTransaction : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid AccountId { get; set; }
    public EducationAccount? Account { get; set; }

    public decimal Amount { get; set; }
    
    /// <summary>
    /// e.g. "TopUp", "Deduction", "Refund"
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;
    
    /// <summary>
    /// e.g. "Manual top-up by Admin", "Payment for Invoice #123"
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
}
