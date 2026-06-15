using System;
using MoePortal.Core.Domain.Enums;

namespace MoePortal.Core.Domain.Entities;

public class EducationAccount : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CitizenId { get; set; }
    public CitizenRecord? CitizenRecord { get; set; }

    public AccountStatus Status { get; set; } = AccountStatus.NotYetCreated;
    public decimal Balance { get; set; } = 0m;

    public DateOnly? OpenedDate { get; set; }
    public DateOnly? ClosedDate { get; set; }
    public string? ClosureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
}
