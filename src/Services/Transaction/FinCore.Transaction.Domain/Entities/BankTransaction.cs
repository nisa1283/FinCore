namespace FinCore.Transaction.Domain.Entities;

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

public class BankTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Aynı kullanıcı aynı anahtarla ikinci kez işlem yapamaz (DB'de unique index)
    public string IdempotencyKey { get; set; } = string.Empty;

    public Guid SenderUserId { get; set; }
    public Guid SourceAccountId { get; set; }
    public string? SourceAccountNumber { get; set; }

    public Guid? ReceiverUserId { get; set; }
    public Guid? TargetAccountId { get; set; }
    public string TargetAccountNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string? Currency { get; set; }          // Account Service cevap verince dolar
    public string? Description { get; set; }
    public string Category { get; set; } = "General";

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? FailureReason { get; set; }

    public int RiskScore { get; set; }
    public bool IsSuspicious { get; set; }
    public string? RiskReasons { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}