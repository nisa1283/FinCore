using FinCore.Transaction.Application.DTOs;

namespace FinCore.Transaction.Application.Services;

public interface ITransactionService
{
    Task<TransferResponse> TransferAsync(Guid userId, string idempotencyKey, TransferRequest request);
    Task<TransferResponse> GetByIdAsync(Guid userId, Guid transactionId);
}