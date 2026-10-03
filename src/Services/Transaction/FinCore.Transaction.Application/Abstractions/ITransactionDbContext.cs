using FinCore.Transaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Transaction.Application.Abstractions;

public interface ITransactionDbContext
{
    DbSet<BankTransaction> Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}