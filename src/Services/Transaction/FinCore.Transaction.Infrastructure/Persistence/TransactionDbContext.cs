using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Transaction.Infrastructure.Persistence;

public class TransactionDbContext : DbContext, ITransactionDbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<BankTransaction> Transactions => Set<BankTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankTransaction>(e =>
        {
            e.ToTable("Transactions");
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();

            // Son güvenlik ağı: Redis'e bir şey olsa bile aynı anahtarla ikinci kayıt atılamaz
            e.HasIndex(x => new { x.SenderUserId, x.IdempotencyKey }).IsUnique();

            e.HasIndex(x => x.SenderUserId);
            e.HasIndex(x => x.ReceiverUserId);
            e.HasIndex(x => x.CreatedAt);

            e.Property(x => x.SourceAccountNumber).HasMaxLength(34);
            e.Property(x => x.TargetAccountNumber).HasMaxLength(34).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.FailureReason).HasMaxLength(300);
            e.Property(x => x.RiskReasons).HasMaxLength(200);
            e.HasIndex(x => x.IsSuspicious);
        });
    }
}