using System.Security.Cryptography;
using FinCore.Account.Application.Abstractions;
using FinCore.Account.Application.DTOs;
using FinCore.Account.Domain.Entities;
using FinCore.BuildingBlocks.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Account.Application.Services;

public class AccountService : IAccountService
{
    private const int MaxAccountsPerUser = 5;
    private const decimal DemoOpeningBalance = 10_000m; // Demo proje: yeni hesaba başlangıç bakiyesi

    private readonly IAccountDbContext _db;
    private readonly IValidator<CreateAccountRequest> _createValidator;

    public AccountService(IAccountDbContext db, IValidator<CreateAccountRequest> createValidator)
    {
        _db = db;
        _createValidator = createValidator;
    }

    public async Task<AccountResponse> CreateAsync(Guid userId, CreateAccountRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var count = await _db.Accounts.CountAsync(a => a.UserId == userId);
        if (count >= MaxAccountsPerUser)
            throw new BusinessRuleException($"You can have at most {MaxAccountsPerUser} accounts.");

        var account = new BankAccount
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            AccountNumber = await GenerateUniqueAccountNumberAsync(),
            Balance = DemoOpeningBalance
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        return ToResponse(account);
    }

    public async Task<List<AccountResponse>> GetMyAccountsAsync(Guid userId)
    {
        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        return accounts.Select(ToResponse).ToList();
    }

    public async Task<AccountResponse> GetByIdAsync(Guid userId, bool isAdmin, Guid accountId)
    {
        var account = await FindAccessibleAccountAsync(userId, isAdmin, accountId);
        return ToResponse(account);
    }

    public async Task<AccountResponse> SetStatusAsync(Guid userId, bool isAdmin, Guid accountId, AccountStatus newStatus)
    {
        var account = await FindAccessibleAccountAsync(userId, isAdmin, accountId);

        if (account.Status == newStatus)
            throw new BusinessRuleException($"Account is already {newStatus.ToString().ToLowerInvariant()}.");

        account.Status = newStatus;
        await _db.SaveChangesAsync();

        return ToResponse(account);
    }

    // Hesap yoksa da başkasınınsa da aynı hatayı veriyorum: başkasının hesabının var olduğunu sızdırılmasın.
    private async Task<BankAccount> FindAccessibleAccountAsync(Guid userId, bool isAdmin, Guid accountId)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);

        if (account is null || (!isAdmin && account.UserId != userId))
            throw new NotFoundException("Account not found.");

        return account;
    }

    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var digits = new char[24];
            for (var i = 0; i < digits.Length; i++)
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));

            var number = "TR" + new string(digits);

            if (!await _db.Accounts.AnyAsync(a => a.AccountNumber == number))
                return number;
        }

        throw new ConflictException("Could not generate an account number. Please try again.");
    }

    private static AccountResponse ToResponse(BankAccount a) =>
        new(a.Id, a.AccountNumber, a.Name, a.Currency, a.Balance, a.Status.ToString(), a.CreatedAt);
}