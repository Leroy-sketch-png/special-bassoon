using MoePortal.Core.Domain;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Exceptions;
using Xunit;
using System;

namespace MoePortal.Tests;

public class SplitPaymentCalculatorTests
{
    [Fact]
    public void CalculateSplit_EaGreater_TakesFullAmount()
    {
        var citizen = new CitizenRecord { EducationAccount = new EducationAccount { Status = AccountStatus.Active, Balance = 1000m } };
        var (fromEa, fromPsp) = SplitPaymentCalculator.CalculateSplit(citizen, 500m);
        Assert.Equal(500m, fromEa);
        Assert.Equal(0m, fromPsp);
    }

    [Fact]
    public void CalculateSplit_EaEqual_TakesFullAmount()
    {
        var citizen = new CitizenRecord { EducationAccount = new EducationAccount { Status = AccountStatus.Active, Balance = 500m } };
        var (fromEa, fromPsp) = SplitPaymentCalculator.CalculateSplit(citizen, 500m);
        Assert.Equal(500m, fromEa);
        Assert.Equal(0m, fromPsp);
    }

    [Fact]
    public void CalculateSplit_EaLess_TakesPartialAmount()
    {
        var citizen = new CitizenRecord { EducationAccount = new EducationAccount { Status = AccountStatus.Active, Balance = 200m } };
        var (fromEa, fromPsp) = SplitPaymentCalculator.CalculateSplit(citizen, 500m);
        Assert.Equal(200m, fromEa);
        Assert.Equal(300m, fromPsp);
    }

    [Fact]
    public void CalculateSplit_EaZero_TakesZero()
    {
        var citizen = new CitizenRecord { EducationAccount = new EducationAccount { Status = AccountStatus.Active, Balance = 0m } };
        var (fromEa, fromPsp) = SplitPaymentCalculator.CalculateSplit(citizen, 500m);
        Assert.Equal(0m, fromEa);
        Assert.Equal(500m, fromPsp);
    }

    [Fact]
    public void CalculateSplit_NoEa_TakesZero()
    {
        var citizen = new CitizenRecord { EducationAccount = null };
        var (fromEa, fromPsp) = SplitPaymentCalculator.CalculateSplit(citizen, 500m);
        Assert.Equal(0m, fromEa);
        Assert.Equal(500m, fromPsp);
    }

    [Fact]
    public void CalculateSplit_NegativeAmount_Throws()
    {
        var citizen = new CitizenRecord { EducationAccount = new EducationAccount { Status = AccountStatus.Active, Balance = 100m } };
        Assert.Throws<DomainException>(() => SplitPaymentCalculator.CalculateSplit(citizen, -500m));
    }
}
