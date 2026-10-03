using FinCore.Account.Application.Abstractions;
using FinCore.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Account.Infrastructure.Persistence;

public class AccountDbContext : DbContext, IAccountDbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    public DbSet<BankAccount> Accounts => Set<BankAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankAccount>(e =>
        {
            e.ToTable("Accounts");
            e.Property(x => x.AccountNumber).HasMaxLength(34).IsRequired();
            e.HasIndex(x => x.AccountNumber).IsUnique();
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); // DB'de "Active"/"Frozen" olarak okunur
            e.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}