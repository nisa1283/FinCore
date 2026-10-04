using FinCore.Account.Application.Abstractions;
using FinCore.Account.Application.DTOs;
using FinCore.Account.Domain.Entities;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.BuildingBlocks.Responses;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Account.Application.Services;

public class AdminAccountService : IAdminAccountService
{
    private readonly IAccountDbContext _db;

    public AdminAccountService(IAccountDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AdminAccountItem>> GetAccountsAsync(AdminAccountQuery q)
    {
        var (page, pageSize) = Paging.Normalize(q.Page, q.PageSize);

        var query = _db.Accounts.AsNoTracking();

        if (q.UserId.HasValue)
            query = query.Where(a => a.UserId == q.UserId.Value);

        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            if (!Enum.TryParse<AccountStatus>(q.Status, true, out var status) || !Enum.IsDefined(status))
                throw new BusinessRuleException("Status must be Active or Frozen.");

            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            query = query.Where(a => a.AccountNumber.Contains(search) || a.Name.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AdminAccountItem>
        {
            Items = items.Select(a => new AdminAccountItem(
                a.Id, a.UserId, a.AccountNumber, a.Name, a.Currency,
                a.Balance, a.Status.ToString(), a.CreatedAt)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}