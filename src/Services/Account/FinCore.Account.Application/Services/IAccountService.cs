using FinCore.Account.Application.DTOs;
using FinCore.Account.Domain.Entities;

namespace FinCore.Account.Application.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(Guid userId, CreateAccountRequest request);
    Task<List<AccountResponse>> GetMyAccountsAsync(Guid userId);
    Task<AccountResponse> GetByIdAsync(Guid userId, bool isAdmin, Guid accountId);
    Task<AccountResponse> SetStatusAsync(Guid userId, bool isAdmin, Guid accountId, AccountStatus newStatus);
}