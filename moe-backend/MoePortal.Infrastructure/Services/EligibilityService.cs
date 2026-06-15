using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Exceptions;
using MoePortal.Core.Interfaces;
using MoePortal.Infrastructure.Data;

namespace MoePortal.Infrastructure.Services;

public class EligibilityService : IEligibilityService
{
    private readonly AppDbContext _db;
    private readonly ICpfPayoutService _cpfPayoutService;

    public EligibilityService(AppDbContext db, ICpfPayoutService cpfPayoutService)
    {
        _db = db;
        _cpfPayoutService = cpfPayoutService;
    }

    public async Task<CitizenRecord> EvaluateAndApplyLifecycleAsync(Guid citizenId, CancellationToken ct = default)
    {
        var record = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .FirstOrDefaultAsync(c => c.Id == citizenId, ct)
            ?? throw new DomainException("CITIZEN_NOT_FOUND", "Citizen record not found.");

        record.EducationAccount ??= new EducationAccount { CitizenId = citizenId };

        if (record.IsDeceased)
        {
            if (record.EducationAccount.Status != AccountStatus.Closed)
            {
                record.EducationAccount.Status = AccountStatus.Closed;
                record.EducationAccount.ClosedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                record.EducationAccount.ClosureReason = "Deceased";
                await _db.SaveChangesAsync(ct);
            }
            return record;
        }

        int age = record.AgeAsOf(DateOnly.FromDateTime(DateTime.UtcNow));

        if (age >= 16 && record.EducationAccount.Status == AccountStatus.NotYetCreated)
        {
            record.EducationAccount.Status = AccountStatus.Active;
            record.EducationAccount.OpenedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            await _db.SaveChangesAsync(ct);
        }
        else if (age >= 30 && record.EducationAccount.Status == AccountStatus.Active)
        {
            var payoutBalance = record.EducationAccount.Balance;
            record.EducationAccount.Balance = 0;
            record.EducationAccount.Status = AccountStatus.Closed;
            record.EducationAccount.ClosedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            record.EducationAccount.ClosureReason = "Age Limit Reached";
            
            if (payoutBalance > 0)
            {
                var transaction = new EducationAccountTransaction
                {
                    AccountId = record.EducationAccount.Id,
                    Amount = -payoutBalance,
                    TransactionType = "TransferOut",
                    Description = "Transferred to CPF Ordinary Account"
                };
                _db.EducationAccountTransactions.Add(transaction);
                
                await _cpfPayoutService.GenerateIso20022PayoutFileAsync(record, payoutBalance, ct);
            }

            await _db.SaveChangesAsync(ct);
        }

        return record;
    }

    public async Task<CitizenRecord> AdminOverrideStatusAsync(Guid citizenId, AccountStatus newStatus, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required for admin overrides.", nameof(reason));

        var record = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .FirstOrDefaultAsync(c => c.Id == citizenId, ct)
            ?? throw new DomainException("CITIZEN_NOT_FOUND", "Citizen record not found.");

        record.EducationAccount ??= new EducationAccount { CitizenId = citizenId };
        record.EducationAccount.Status = newStatus;
        
        if (newStatus == AccountStatus.Closed)
        {
            record.EducationAccount.ClosedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            record.EducationAccount.ClosureReason = $"Admin Override: {reason}";
        }

        var action = new ManualAccountAction
        {
            AccountId = record.EducationAccount.Id,
            ActionType = "StatusOverride",
            Reason = reason,
            Amount = 0
        };
        _db.ManualAccountActions.Add(action);

        var transaction = new EducationAccountTransaction
        {
            AccountId = record.EducationAccount.Id,
            Amount = 0,
            TransactionType = "StatusOverride",
            Description = $"Admin changed status to {newStatus}. Reason: {reason}"
        };
        _db.EducationAccountTransactions.Add(transaction);

        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<CitizenRecord> ManualCreateAccountAsync(Guid citizenId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required for manual account creation.", nameof(reason));

        var record = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .FirstOrDefaultAsync(c => c.Id == citizenId, ct)
            ?? throw new DomainException("CITIZEN_NOT_FOUND", "Citizen record not found.");

        if (record.EducationAccount != null && record.EducationAccount.Status != AccountStatus.NotYetCreated)
            throw new DomainException("ACCOUNT_ALREADY_EXISTS", "Account already exists for this citizen.");

        record.EducationAccount ??= new EducationAccount { CitizenId = citizenId };
        record.EducationAccount.Status = AccountStatus.Active;
        record.EducationAccount.OpenedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var action = new ManualAccountAction
        {
            AccountId = record.EducationAccount.Id,
            ActionType = "ManualCreate",
            Reason = reason,
            Amount = 0
        };
        _db.ManualAccountActions.Add(action);

        var transaction = new EducationAccountTransaction
        {
            AccountId = record.EducationAccount.Id,
            Amount = 0,
            TransactionType = "ManualCreate",
            Description = $"Admin manually created account. Reason: {reason}"
        };
        _db.EducationAccountTransactions.Add(transaction);

        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<ManualAccountAction> ProposeAdminTopUpAsync(Guid citizenId, decimal amount, string reason, string makerId, CancellationToken ct = default)
    {
        if (amount <= 0) throw new DomainException("INVALID_AMOUNT", "Top-up amount must be positive.");

        var record = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .FirstOrDefaultAsync(c => c.Id == citizenId, ct)
            ?? throw new DomainException("CITIZEN_NOT_FOUND", "Citizen record not found.");

        record.EducationAccount ??= new EducationAccount { CitizenId = citizenId };

        if (record.EducationAccount.Status != AccountStatus.Active)
            throw new DomainException("ACCOUNT_INACTIVE", "Cannot top-up an inactive account.");

        var action = new ManualAccountAction
        {
            AccountId = record.EducationAccount.Id,
            ActionType = "TopUp",
            Reason = reason,
            Amount = amount,
            Status = "PendingChecker",
            MakerId = makerId
        };
        _db.ManualAccountActions.Add(action);
        await _db.SaveChangesAsync(ct);
        return action;
    }

    public async Task<CitizenRecord> ApproveAdminTopUpAsync(Guid actionId, string checkerId, bool isApproved, CancellationToken ct = default)
    {
        var action = await _db.ManualAccountActions.Include(a => a.Account).ThenInclude(acc => acc!.CitizenRecord).FirstOrDefaultAsync(a => a.Id == actionId, ct)
            ?? throw new DomainException("ACTION_NOT_FOUND", "Manual account action not found.");
            
        if (action.Status != "PendingChecker") throw new DomainException("INVALID_STATE", "Action is not pending checker.");
        if (action.MakerId == checkerId) throw new DomainException("MAKER_CHECKER_CONFLICT", "Maker cannot be the checker.");
        
        action.Status = isApproved ? "Approved" : "Rejected";
        action.CheckerId = checkerId;
        
        if (isApproved && action.Account != null)
        {
            action.Account.Balance += action.Amount;
            var transaction = new EducationAccountTransaction
            {
                AccountId = action.Account.Id,
                Amount = action.Amount,
                TransactionType = action.ActionType,
                Description = $"Manual top-up approved by checker. Reason: {action.Reason}"
            };
            _db.EducationAccountTransactions.Add(transaction);
        }
        
        await _db.SaveChangesAsync(ct);
        return action.Account!.CitizenRecord!;
    }

    public async Task<CitizenRecord> SystemTopUpAsync(Guid citizenId, decimal amount, string reason, CancellationToken ct = default)
    {
        if (amount <= 0) throw new DomainException("INVALID_AMOUNT", "Top-up amount must be positive.");

        var record = await _db.CitizenRecords
            .Include(c => c.EducationAccount)
            .FirstOrDefaultAsync(c => c.Id == citizenId, ct)
            ?? throw new DomainException("CITIZEN_NOT_FOUND", "Citizen record not found.");

        record.EducationAccount ??= new EducationAccount { CitizenId = citizenId };

        if (record.EducationAccount.Status != AccountStatus.Active)
            throw new DomainException("ACCOUNT_INACTIVE", "Cannot top-up an inactive account.");

        record.EducationAccount.Balance += amount;

        var action = new ManualAccountAction
        {
            AccountId = record.EducationAccount.Id,
            ActionType = "TopUp",
            Reason = "System TopUp: " + reason,
            Amount = amount,
            Status = "Approved",
            MakerId = "System",
            CheckerId = "System"
        };
        _db.ManualAccountActions.Add(action);

        var transaction = new EducationAccountTransaction
        {
            AccountId = record.EducationAccount.Id,
            Amount = amount,
            TransactionType = "TopUp",
            Description = $"System top-up. Reason: {reason}"
        };
        _db.EducationAccountTransactions.Add(transaction);

        await _db.SaveChangesAsync(ct);
        return record;
    }
}
