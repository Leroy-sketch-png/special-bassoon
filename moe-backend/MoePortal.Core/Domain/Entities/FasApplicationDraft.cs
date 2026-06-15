using MoePortal.Core.Domain.Enums;

namespace MoePortal.Core.Domain.Entities;

public class FasApplicationDraft : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CitizenRecordId { get; set; }

    /// <summary>JSON-serialized List of {role, content} messages. Max 50 turns.</summary>
    public string ConversationHistoryJson { get; set; } = "[]";

    /// <summary>JSON-serialized partial FAS application object. Updated each turn.</summary>
    public string DraftFieldsJson { get; set; } = "{}";

    public FasDraftStatus Status { get; set; } = FasDraftStatus.InProgress;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    public CitizenRecord CitizenRecord { get; set; } = null!;
}
