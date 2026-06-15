using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Exceptions;
using MoePortal.Core.Interfaces;
using MoePortal.Infrastructure.Data;
using MoePortal.Infrastructure.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MoePortal.Tests;

public class EligibilityServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public EligibilityServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Close();

    private AppDbContext CreateContext() => new AppDbContext(_options);

    private class DummyCpfPayoutService : ICpfPayoutService
    {
        public Task GenerateIso20022PayoutFileAsync(CitizenRecord citizen, decimal amount, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    // ── Auto-create at 16 ────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateLifecycle_Age16_CreatesAccount()
    {
        using var ctx = CreateContext();
        var id = Guid.NewGuid();
        ctx.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = id,
            Nric                   = "T1111111A",
            FullName               = "Sixteen Test",
            DateOfBirth            = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16).AddDays(-1)),
            EducationAccount       = new EducationAccount { Status = AccountStatus.NotYetCreated }
        });
        await ctx.SaveChangesAsync();

        var result = await new EligibilityService(ctx, new DummyCpfPayoutService()).EvaluateAndApplyLifecycleAsync(id);

        Assert.Equal(AccountStatus.Active, result.EducationAccount!.Status);
        Assert.NotNull(result.EducationAccount.OpenedDate);
    }

    [Fact]
    public async Task EvaluateLifecycle_Under16_NoChange()
    {
        using var ctx = CreateContext();
        var id = Guid.NewGuid();
        ctx.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = id,
            Nric                   = "T2222222B",
            FullName               = "Fourteen Test",
            DateOfBirth            = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-14)),
            EducationAccount       = new EducationAccount { Status = AccountStatus.NotYetCreated }
        });
        await ctx.SaveChangesAsync();

        var result = await new EligibilityService(ctx, new DummyCpfPayoutService()).EvaluateAndApplyLifecycleAsync(id);

        Assert.Equal(AccountStatus.NotYetCreated, result.EducationAccount!.Status);
        Assert.Null(result.EducationAccount.OpenedDate);
    }

    // ── Auto-close at 30 ─────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateLifecycle_Age30_ClosesAccount()
    {
        using var ctx = CreateContext();
        var id = Guid.NewGuid();
        ctx.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = id,
            Nric                   = "T3333333C",
            FullName               = "Thirty Test",
            DateOfBirth            = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30).AddDays(-1)),
            EducationAccount       = new EducationAccount { Status = AccountStatus.Active }
        });
        await ctx.SaveChangesAsync();

        var result = await new EligibilityService(ctx, new DummyCpfPayoutService()).EvaluateAndApplyLifecycleAsync(id);

        Assert.Equal(AccountStatus.Closed, result.EducationAccount!.Status);
        Assert.NotNull(result.EducationAccount.ClosedDate);
        Assert.Contains("Age", result.EducationAccount.ClosureReason);
    }

    // ── Death closure ────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateLifecycle_Deceased_ClosesImmediately()
    {
        using var ctx = CreateContext();
        var id = Guid.NewGuid();
        ctx.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = id,
            Nric                   = "T4444444D",
            FullName               = "Deceased Test",
            DateOfBirth            = new DateOnly(2000, 1, 1),
            DateOfDeath            = new DateOnly(2026, 6, 1),
            EducationAccount       = new EducationAccount { Status = AccountStatus.Active }
        });
        await ctx.SaveChangesAsync();

        var result = await new EligibilityService(ctx, new DummyCpfPayoutService()).EvaluateAndApplyLifecycleAsync(id);

        Assert.Equal(AccountStatus.Closed, result.EducationAccount!.Status);
        Assert.Contains("Deceased", result.EducationAccount.ClosureReason, StringComparison.OrdinalIgnoreCase);
    }

    // ── Already closed — idempotent ──────────────────────────────────────────

    [Fact]
    public async Task EvaluateLifecycle_AlreadyClosed_NoChange()
    {
        using var ctx = CreateContext();
        var id = Guid.NewGuid();
        ctx.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = id,
            Nric                   = "T5555555E",
            FullName               = "Already Closed",
            DateOfBirth            = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            EducationAccount       = new EducationAccount 
            { 
                Status = AccountStatus.Closed,
                ClosureReason   = "Manual close test"
            }
        });
        await ctx.SaveChangesAsync();

        var result = await new EligibilityService(ctx, new DummyCpfPayoutService()).EvaluateAndApplyLifecycleAsync(id);

        Assert.Equal(AccountStatus.Closed, result.EducationAccount!.Status);
        Assert.Equal("Manual close test", result.EducationAccount.ClosureReason); // Not overwritten
    }

    // ── Admin override ───────────────────────────────────────────────────────

    [Fact]
    public async Task AdminOverride_RequiresReason()
    {
        using var ctx = CreateContext();
        var id = Guid.NewGuid();
        ctx.CitizenRecords.Add(new CitizenRecord
        {
            Id      = id,
            Nric    = "T6666666F",
            FullName = "Override Test",
            DateOfBirth = new DateOnly(2005, 1, 1),
            EducationAccount = new EducationAccount { Status = AccountStatus.Active }
        });
        await ctx.SaveChangesAsync();

        var svc = new EligibilityService(ctx, new DummyCpfPayoutService());

        // Without reason should throw
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.AdminOverrideStatusAsync(id, AccountStatus.Closed, ""));

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.AdminOverrideStatusAsync(id, AccountStatus.Closed, "   "));

        // Override to Closed with reason
        var result = await svc.AdminOverrideStatusAsync(id, AccountStatus.Closed, "Fraud investigation");
        Assert.Equal(AccountStatus.Closed, result.EducationAccount!.Status);
        Assert.Contains("Fraud investigation", result.EducationAccount.ClosureReason);
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateLifecycle_NotFound_ThrowsDomainException()
    {
        using var ctx = CreateContext();
        await Assert.ThrowsAsync<DomainException>(
            () => new EligibilityService(ctx, new DummyCpfPayoutService()).EvaluateAndApplyLifecycleAsync(Guid.NewGuid()));
    }

    // ── Legacy test from original file ───────────────────────────────────────

    [Fact]
    public void SplitPaymentCalculator_LegacyTest_SplitsCorrectly()
    {
        var citizen = new CitizenRecord
        {
            EducationAccount = new EducationAccount 
            {
                Status  = AccountStatus.Active,
                Balance = 300m
            }
        };
        var (fromEdusave, fromPsp) = SplitPaymentCalculator.CalculateSplit(citizen, 500m);
        Assert.Equal(300m, fromEdusave);
        Assert.Equal(200m, fromPsp);
    }
}
