using FinCore.BuildingBlocks.Responses;
using FinCore.Transaction.Application.DTOs;

namespace FinCore.Transaction.Application.Services;

public interface ITransactionQueryService
{
    Task<PagedResult<TransactionHistoryItem>> GetMyTransactionsAsync(Guid userId, TransactionQuery query);
    Task<PagedResult<AdminTransactionItem>> GetAdminTransactionsAsync(TransactionQuery query);
    Task<TransactionStatsResponse> GetStatsAsync();
    Task<TransactionSummaryResponse> GetSummaryAsync(Guid userId, string currency, int months);
}