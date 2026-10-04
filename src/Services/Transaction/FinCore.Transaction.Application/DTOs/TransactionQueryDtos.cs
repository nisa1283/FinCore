namespace FinCore.Transaction.Application.DTOs;

// URL'den gelir: /api/transactions?page=1&pageSize=10&status=Completed&type=Outgoing ...
public class TransactionQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Status { get; set; }        // Pending, Completed, Failed
    public string? Category { get; set; }
    public string? Type { get; set; }          // Incoming, Outgoing (sadece kullanıcı geçmişinde)
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? Search { get; set; }        // açıklamada arar
    public bool SuspiciousOnly { get; set; }   // sadece admin
}

public record TransactionHistoryItem(
    Guid Id,
    DateTime Date,
    string? Description,
    string Category,
    decimal Amount,
    string? Currency,
    string Status,
    string Type,
    string? CounterpartyAccountNumber,
    string? FailureReason);

public record AdminTransactionItem(
    Guid Id,
    DateTime Date,
    Guid SenderUserId,
    Guid? ReceiverUserId,
    string? SourceAccountNumber,
    string TargetAccountNumber,
    decimal Amount,
    string? Currency,
    string Category,
    string? Description,
    string Status,
    string? FailureReason,
    int RiskScore,
    string? RiskReasons,
    bool IsSuspicious);

public record CurrencyVolume(string Currency, decimal Total);

public record TransactionStatsResponse(
    int TotalCount,
    int CompletedCount,
    int FailedCount,
    int SuspiciousCount,
    List<CurrencyVolume> VolumeByCurrency);