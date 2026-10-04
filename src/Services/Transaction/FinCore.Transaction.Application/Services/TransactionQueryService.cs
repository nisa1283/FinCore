using FinCore.BuildingBlocks.Exceptions;
using FinCore.BuildingBlocks.Responses;
using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Transaction.Application.Services;

public class TransactionQueryService : ITransactionQueryService
{
    private readonly ITransactionDbContext _db;

    public TransactionQueryService(ITransactionDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TransactionHistoryItem>> GetMyTransactionsAsync(Guid userId, TransactionQuery q)
    {
        var (page, pageSize) = Paging.Normalize(q.Page, q.PageSize);

        // Kullanıcı hem gönderdiği hem aldığı işlemleri görür
        var query = _db.Transactions.AsNoTracking()
            .Where(t => t.SenderUserId == userId || t.ReceiverUserId == userId);

        var type = q.Type?.Trim().ToLowerInvariant();
        if (type == "outgoing")
            query = query.Where(t => t.SenderUserId == userId);
        else if (type == "incoming")
            query = query.Where(t => t.ReceiverUserId == userId);
        else if (!string.IsNullOrEmpty(type))
            throw new BusinessRuleException("Type must be 'Incoming' or 'Outgoing'.");

        query = ApplyFilters(query, q);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TransactionHistoryItem>
        {
            Items = items.Select(t => ToHistoryItem(t, userId)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<AdminTransactionItem>> GetAdminTransactionsAsync(TransactionQuery q)
    {
        var (page, pageSize) = Paging.Normalize(q.Page, q.PageSize);

        var query = ApplyFilters(_db.Transactions.AsNoTracking(), q);

        if (q.SuspiciousOnly)
            query = query.Where(t => t.IsSuspicious);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AdminTransactionItem>
        {
            Items = items.Select(ToAdminItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<TransactionStatsResponse> GetStatsAsync()
    {
        var all = _db.Transactions.AsNoTracking();

        var total = await all.CountAsync();
        var completed = await all.CountAsync(t => t.Status == TransactionStatus.Completed);
        var failed = await all.CountAsync(t => t.Status == TransactionStatus.Failed);
        var suspicious = await all.CountAsync(t => t.IsSuspicious);

        var volumes = await all
            .Where(t => t.Status == TransactionStatus.Completed && t.Currency != null)
            .GroupBy(t => t.Currency!)
            .Select(g => new { Currency = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        return new TransactionStatsResponse(
            total, completed, failed, suspicious,
            volumes.Select(v => new CurrencyVolume(v.Currency, v.Total)).ToList());
    }

    private static IQueryable<BankTransaction> ApplyFilters(IQueryable<BankTransaction> query, TransactionQuery q)
    {
        if (q.From.HasValue)
        {
            var from = q.From.Value.Date;
            query = query.Where(t => t.CreatedAt >= from);
        }

        if (q.To.HasValue)
        {
            var toExclusive = q.To.Value.Date.AddDays(1); // "to" günü de dahil olsun
            query = query.Where(t => t.CreatedAt < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            if (!Enum.TryParse<TransactionStatus>(q.Status, true, out var status) || !Enum.IsDefined(status))
                throw new BusinessRuleException("Status must be Pending, Completed or Failed.");

            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(q.Category))
        {
            var category = q.Category.Trim();
            query = query.Where(t => t.Category == category);
        }

        if (q.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= q.MinAmount.Value);

        if (q.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= q.MaxAmount.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            query = query.Where(t => t.Description != null && t.Description.Contains(search));
        }

        return query;
    }

    private static TransactionHistoryItem ToHistoryItem(BankTransaction t, Guid userId)
    {
        var isOutgoing = t.SenderUserId == userId;

        return new TransactionHistoryItem(
            t.Id,
            t.CreatedAt,
            t.Description,
            t.Category,
            t.Amount,
            t.Currency,
            t.Status.ToString(),
            isOutgoing ? "Outgoing" : "Incoming",
            isOutgoing ? t.TargetAccountNumber : t.SourceAccountNumber,
            isOutgoing ? t.FailureReason : null);
    }

    private static AdminTransactionItem ToAdminItem(BankTransaction t) => new(
        t.Id, t.CreatedAt, t.SenderUserId, t.ReceiverUserId,
        t.SourceAccountNumber, t.TargetAccountNumber,
        t.Amount, t.Currency, t.Category, t.Description,
        t.Status.ToString(), t.FailureReason,
        t.RiskScore, t.RiskReasons, t.IsSuspicious);
}