using FinCore.Account.Application.Abstractions;
using FinCore.Account.Domain.Entities;
using FinCore.BuildingBlocks.Contracts;
using FinCore.BuildingBlocks.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Account.Application.Services;

public class FundsTransferService : IFundsTransferService
{
    private readonly IAccountDbContext _db;

    public FundsTransferService(IAccountDbContext db)
    {
        _db = db;
    }

    public async Task<InternalTransferResponse> TransferAsync(InternalTransferRequest request)
    {
        var amount = Math.Round(request.Amount, 2);
        if (amount <= 0)
            throw new BusinessRuleException("Amount must be greater than zero.");

        var source = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.SourceAccountId)
            ?? throw new NotFoundException("Source account not found.");

        if (source.UserId != request.RequestingUserId)
            throw new ForbiddenException("You can only transfer from your own accounts.");

        var targetNumber = request.TargetAccountNumber.Trim().ToUpperInvariant();
        var target = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == targetNumber)
            ?? throw new NotFoundException("Target account not found.");

        if (source.Id == target.Id)
            throw new BusinessRuleException("Source and target accounts must be different.");

        if (source.Status == AccountStatus.Frozen)
            throw new BusinessRuleException("Source account is frozen.");

        if (target.Status == AccountStatus.Frozen)
            throw new BusinessRuleException("Target account is frozen.");

        if (source.Currency != target.Currency)
            throw new BusinessRuleException("Transfers between different currencies are not supported.");

        if (source.Balance < amount)
            throw new BusinessRuleException("Insufficient balance.");

        source.Balance -= amount;
        target.Balance += amount;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Bu iki hesap, biz işlem yaparken başka bir işlem tarafından değiştirildi
            throw new ConflictException("Accounts were modified by another operation. Please try again.");
        }

        return new InternalTransferResponse(
            source.Id,
            source.AccountNumber,
            source.UserId,
            target.Id,
            target.AccountNumber,
            target.UserId,
            source.Currency,
            source.Balance);
    }
}