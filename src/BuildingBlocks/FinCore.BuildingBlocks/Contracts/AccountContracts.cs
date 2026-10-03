namespace FinCore.BuildingBlocks.Contracts;

// Transaction Service → Account Service: "şu hesaptan şu hesaba şu kadar para taşı"
public record InternalTransferRequest(
    Guid RequestingUserId,
    Guid SourceAccountId,
    string TargetAccountNumber,
    decimal Amount,
    Guid TransactionId);

// Account Service → Transaction Service: işlemin sonucu
public record InternalTransferResponse(
    Guid SourceAccountId,
    string SourceAccountNumber,
    Guid SenderUserId,
    Guid TargetAccountId,
    string TargetAccountNumber,
    Guid ReceiverUserId,
    string Currency,
    decimal SourceBalanceAfter);