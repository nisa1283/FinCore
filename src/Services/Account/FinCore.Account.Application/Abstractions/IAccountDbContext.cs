using FinCore.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Account.Application.Abstractions;

public interface IAccountDbContext
{
    DbSet<BankAccount> Accounts { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}