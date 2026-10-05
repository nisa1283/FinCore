using FinCore.BuildingBlocks.Exceptions;
using FinCore.BuildingBlocks.Responses;
using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
    public async Task<TransactionSummaryResponse> GetSummaryAsync(Guid userId, string currency, int months)
    {
        currency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new BusinessRuleException("Currency must be a 3-letter code such as TRY.");

        months = Math.Clamp(months, 1, 12);

        // İçinde bulunduğumuz ay dahil, geriye doğru "months" kadar ayın ilk günü
        var now = DateTime.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));

        // Sadece tamamlanmış işlemler. Kendi hesapları arasındaki transferler gelir/gider sayılmaz.
        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Status == TransactionStatus.Completed
                     && t.Currency == currency
                     && t.CreatedAt >= firstMonth
                     && (t.SenderUserId == userId || t.ReceiverUserId == userId)
                     && t.ReceiverUserId != t.SenderUserId)
            .Select(t => new { t.CreatedAt, t.Amount, t.Category, Outgoing = t.SenderUserId == userId })
            .ToListAsync();

        var monthly = Enumerable.Range(0, months)
            .Select(i => firstMonth.AddMonths(i))
            .Select(start => new MonthlyFlow(
                start.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                rows.Where(r => r.CreatedAt.Year == start.Year && r.CreatedAt.Month == start.Month && !r.Outgoing)
                    .Sum(r => r.Amount),
                rows.Where(r => r.CreatedAt.Year == start.Year && r.CreatedAt.Month == start.Month && r.Outgoing)
                    .Sum(r => r.Amount)))
            .ToList();

        var categories = rows
            .Where(r => r.Outgoing)
            .GroupBy(r => r.Category)
            .Select(g => new CategorySpend(g.Key, g.Sum(r => r.Amount)))
            .OrderByDescending(c => c.Total)
            .ToList();

        return new TransactionSummaryResponse(
            currency,
            monthly.Sum(m => m.Income),
            monthly.Sum(m => m.Expense),
            monthly,
            categories);
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
        if (q.AccountId.HasValue)
        {
            var accountId = q.AccountId.Value;
            query = query.Where(t => t.SourceAccountId == accountId || t.TargetAccountId == accountId);
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