using MoePortal.Core.Domain.Enums;

namespace MoePortal.Core.Domain.Entities;

public class CitizenRecord : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stored encrypted at rest. Never log this field.
    /// </summary>
    public string Nric { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public DateOnly? DateOfDeath { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public EducationAccount? EducationAccount { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<FasApplicationDraft> FasDrafts { get; set; } = new List<FasApplicationDraft>();

    // Domain helpers (pure, no DB access)
    public int AgeAsOf(DateOnly referenceDate) =>
        referenceDate.Year - DateOfBirth.Year -
        (referenceDate.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    public bool IsDeceased => DateOfDeath.HasValue;
}
