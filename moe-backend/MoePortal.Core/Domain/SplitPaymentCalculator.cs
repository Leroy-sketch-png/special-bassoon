using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Exceptions;

namespace MoePortal.Core.Domain;

public static class SplitPaymentCalculator
{
    /// <summary>
    /// Calculates Education Account vs PSP split given the citizen entity.
    /// EA covers as much as possible; remainder goes to PSP.
    /// Returns zero EA portion if account is not Active.
    /// All values in SGD, returned as-is (no rounding — caller should round for display).
    /// </summary>
    public static (decimal FromEdusave, decimal FromPsp) CalculateSplit(CitizenRecord citizen, decimal invoiceTotalAmount)
    {
        if (invoiceTotalAmount <= 0)
            throw new DomainException("INVALID_AMOUNT", "Invoice amount must be greater than zero.");

        if (citizen.EducationAccount == null || citizen.EducationAccount.Status != AccountStatus.Active)
            return (0m, invoiceTotalAmount);

        if (citizen.EducationAccount.Balance >= invoiceTotalAmount)
            return (invoiceTotalAmount, 0m);

        var remainder = invoiceTotalAmount - citizen.EducationAccount.Balance;
        return (citizen.EducationAccount.Balance, remainder);
    }

    /// <summary>
    /// Overload for unit testing and direct computation without a CitizenRecord.
    /// EA balance covers as much as possible. Returns rounded values (2dp).
    /// </summary>
    /// <param name="totalAmount">Invoice total (must be positive)</param>
    /// <param name="eaBalance">Education Account balance (must be non-negative)</param>
    public static (decimal EaPortion, decimal PspPortion) Calculate(decimal totalAmount, decimal eaBalance)
    {
        if (totalAmount <= 0)
            throw new ArgumentException("Total amount must be positive.", nameof(totalAmount));
        if (eaBalance < 0)
            throw new ArgumentException("EA balance cannot be negative.", nameof(eaBalance));

        var eaPortion  = Math.Min(eaBalance, totalAmount);
        var pspPortion = totalAmount - eaPortion;

        return (Math.Round(eaPortion, 2), Math.Round(pspPortion, 2));
    }
}
