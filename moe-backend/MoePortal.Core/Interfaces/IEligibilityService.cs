using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;

namespace MoePortal.Core.Interfaces;

public interface IEligibilityService
{
    /// <summary>
    /// Evaluates lifecycle rules for a citizen and applies any required status transitions.
    /// Rules: Create account at age 16. Close at age 30. Close immediately on death.
    /// </summary>
    Task<CitizenRecord> EvaluateAndApplyLifecycleAsync(Guid citizenId, CancellationToken ct = default);

    /// <summary>Admin override — bypasses lifecycle rules. Requires HQ_ADMIN role (enforced at controller level).</summary>
    Task<CitizenRecord> AdminOverrideStatusAsync(Guid citizenId, AccountStatus newStatus, string reason, CancellationToken ct = default);

    /// <summary>Manual account creation for missing upstream data. Requires Admin.</summary>
    Task<CitizenRecord> ManualCreateAccountAsync(Guid citizenId, string reason, CancellationToken ct = default);

    /// <summary>Manual top-up proposal by admin (Maker). Requires HQ_ADMIN or SCHOOL_ADMIN.</summary>
    Task<ManualAccountAction> ProposeAdminTopUpAsync(Guid citizenId, decimal amount, string reason, string makerId, CancellationToken ct = default);

    /// <summary>Manual top-up approval by admin (Checker). Requires HQ_ADMIN or SCHOOL_ADMIN.</summary>
    Task<CitizenRecord> ApproveAdminTopUpAsync(Guid actionId, string checkerId, bool isApproved, CancellationToken ct = default);

    /// <summary>System automated top-up bypassing maker-checker (e.g. approved FAS grant).</summary>
    Task<CitizenRecord> SystemTopUpAsync(Guid citizenId, decimal amount, string reason, CancellationToken ct = default);
}
