using FinCore.Account.Application.DTOs;
using FinCore.BuildingBlocks.Responses;

namespace FinCore.Account.Application.Services;

public interface IAdminAccountService
{
    Task<PagedResult<AdminAccountItem>> GetAccountsAsync(AdminAccountQuery query);
}