using System;

namespace MoePortal.Core.Domain.Entities;

public class ManualAccountAction : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid AccountId { get; set; }
    public EducationAccount? Account { get; set; }

    public string ActionType { get; set; } = string.Empty; // TopUp, Create, Close, OverrideStatus
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 0m;
    
    // Status: PendingChecker, Approved, Rejected
    public string Status { get; set; } = "PendingChecker";
    
    // Maker-Checker tracking
    public string? MakerId { get; set; }
    public string? CheckerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
}
