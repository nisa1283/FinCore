using FinCore.Account.Application.Abstractions;
using FinCore.Account.Domain.Entities;
using FinCore.Auth.Application.Abstractions;
using FinCore.Auth.Domain.Entities;
using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.UnitTests.Support;

public class TestAccountDbContext : DbContext, IAccountDbContext
{
    public TestAccountDbContext(DbContextOptions<TestAccountDbContext> options) : base(options) { }

    public DbSet<BankAccount> Accounts => Set<BankAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The in-memory provider cannot generate SQL Server row versions
        modelBuilder.Entity<BankAccount>().Ignore(a => a.RowVersion);
    }

    public static TestAccountDbContext Create() =>
        new(new DbContextOptionsBuilder<TestAccountDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

public class TestTransactionDbContext : DbContext, ITransactionDbContext
{
    public TestTransactionDbContext(DbContextOptions<TestTransactionDbContext> options) : base(options) { }

    public DbSet<BankTransaction> Transactions => Set<BankTransaction>();

    public static TestTransactionDbContext Create() =>
        new(new DbContextOptionsBuilder<TestTransactionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

public class TestAuthDbContext : DbContext, IAuthDbContext
{
    public TestAuthDbContext(DbContextOptions<TestAuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public static TestAuthDbContext Create() =>
        new(new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}