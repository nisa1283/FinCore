namespace FinCore.Transaction.Application.DTOs;

public record TransferRequest(
    Guid SourceAccountId,
    string TargetAccountNumber,
    decimal Amount,
    string? Description,
    string? Category);

public record TransferResponse(
    Guid Id,
    string Status,
    decimal Amount,
    string? Currency,
    string? SourceAccountNumber,
    string TargetAccountNumber,
    string? Description,
    string Category,
    string? FailureReason,
    DateTime CreatedAt,
    bool IsDuplicate);