using System;
using System.Text.Json;
using MoePortal.Core.Domain.Enums;

namespace MoePortal.Core.Domain.Entities;

public class FasApplication : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CitizenRecordId { get; set; }
    
    public FasApplicationStatus Status { get; set; } = FasApplicationStatus.PendingReview;
    
    // Storing the snapshot of the application fields
    public string ApplicationDataJson { get; set; } = string.Empty;
    
    // Optional feedback from admin on review
    public string? AdminRemarks { get; set; }
    
    // Maker-Checker tracking
    public string? MakerId { get; set; }
    public string? CheckerId { get; set; }
    
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    public CitizenRecord? CitizenRecord { get; set; }
}
