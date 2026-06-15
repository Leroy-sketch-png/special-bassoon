using MoePortal.Core.Domain;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using Xunit;

namespace MoePortal.Tests;

/// <summary>
/// Tests the split payment calculation logic exhaustively.
/// Theory: verifies all documented edge cases from the plan.
/// </summary>
public class SplitPaymentMathTests
{
    // ── Calculate() overload (raw amount/balance) ───────────────────────────

    [Theory]
    [InlineData(100.00, 200.00, 100.00, 0.00)]   // EA balance > total  → EA covers all
    [InlineData(100.00, 100.00, 100.00, 0.00)]   // EA balance = total  → EA covers exactly
    [InlineData(100.00,  50.00,  50.00, 50.00)]  // EA balance < total  → partial split
    [InlineData(100.00,   0.00,   0.00, 100.00)] // EA balance = 0      → full PSP
    [InlineData(250.75, 100.00, 100.00, 150.75)] // Decimal amounts
    [InlineData(  0.01,   0.01,   0.01,   0.00)] // Minimum amounts
    [InlineData(999.99, 999.99, 999.99,   0.00)] // Large equal amounts
    public void Calculate_ProducesCorrectSplit(
        decimal total, decimal eaBalance, decimal expectedEa, decimal expectedPsp)
    {
        var (ea, psp) = SplitPaymentCalculator.Calculate(total, eaBalance);

        Assert.Equal(expectedEa,  ea);
        Assert.Equal(expectedPsp, psp);
        Assert.Equal(total, ea + psp); // Invariant: portions must always sum to total
    }

    [Fact]
    public void Calculate_NegativeTotal_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SplitPaymentCalculator.Calculate(-1m, 100m));
        Assert.Contains("positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_NegativeBalance_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SplitPaymentCalculator.Calculate(100m, -1m));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── CalculateSplit() overload (with CitizenRecord) ──────────────────────

    [Fact]
    public void CalculateSplit_ActiveAccount_CoversFully()
    {
        var citizen = new CitizenRecord
        {
            EducationAccount = new EducationAccount
            {
                Status  = AccountStatus.Active,
                Balance = 600m
            }
        };
        var (ea, psp) = SplitPaymentCalculator.CalculateSplit(citizen, 500m);
        Assert.Equal(500m, ea);
        Assert.Equal(0m,   psp);
    }

    [Fact]
    public void CalculateSplit_InactiveCitizen_FullPsp()
    {
        // Account not Active (e.g., Closed) → EA portion must be 0
        var citizen = new CitizenRecord
        {
            EducationAccount = new EducationAccount
            {
                Status  = AccountStatus.Closed,
                Balance = 1000m // Balance exists but account closed
            }
        };
        var (ea, psp) = SplitPaymentCalculator.CalculateSplit(citizen, 300m);
        Assert.Equal(0m,   ea);
        Assert.Equal(300m, psp);
    }

    [Fact]
    public void CalculateSplit_ZeroAmount_ThrowsDomainException()
    {
        var citizen = new CitizenRecord { EducationAccount = new EducationAccount { Status = AccountStatus.Active } };
        Assert.Throws<MoePortal.Core.Exceptions.DomainException>(
            () => SplitPaymentCalculator.CalculateSplit(citizen, 0m));
    }
}
