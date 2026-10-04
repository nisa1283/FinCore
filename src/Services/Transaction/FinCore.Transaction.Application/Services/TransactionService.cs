using FinCore.BuildingBlocks.Contracts;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using FinCore.Transaction.Application.Risk;
using FinCore.BuildingBlocks.Events;
using FinCore.BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;

namespace FinCore.Transaction.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionDbContext _db;
    private readonly IAccountClient _accountClient;
    private readonly IIdempotencyService _idempotency;
    private readonly IValidator<TransferRequest> _validator;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionDbContext db,
        IAccountClient accountClient,
        IIdempotencyService idempotency,
        IValidator<TransferRequest> validator,
        IEventPublisher publisher,
        ILogger<TransactionService> logger)
    {
        _db = db;
        _accountClient = accountClient;
        _idempotency = idempotency;
        _validator = validator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<TransferResponse> TransferAsync(Guid userId, string idempotencyKey, TransferRequest request)
    {
        // 1) Girdi kontrolü
        await _validator.ValidateAndThrowAsync(request);

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new BusinessRuleException("Idempotency-Key header is required (max 100 characters).");

        // 2) Idempotency: bu istek daha önce geldiyse yeni işlem yapma, eski sonucu döndür
        var acquired = await _idempotency.TryAcquireAsync(userId, idempotencyKey);
        if (!acquired)
        {
            var existing = await _db.Transactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.SenderUserId == userId && t.IdempotencyKey == idempotencyKey);

            if (existing is null)
                throw new ConflictException("This transfer is already being processed.");

            return ToResponse(existing, isDuplicate: true);
        }

        // 3) Risk skorunu hesapla ve işlemi "Pending" olarak kaydet
        var targetNumber = request.TargetAccountNumber.Trim().ToUpperInvariant();
        BankTransaction transaction;

        try
        {
            var risk = await EvaluateRiskAsync(userId, request.Amount, targetNumber);

            transaction = new BankTransaction
            {
                IdempotencyKey = idempotencyKey,
                SenderUserId = userId,
                SourceAccountId = request.SourceAccountId,
                TargetAccountNumber = targetNumber,
                Amount = request.Amount,
                Description = request.Description?.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category,
                RiskScore = risk.Score,
                RiskReasons = risk.Reasons.Count > 0 ? string.Join(",", risk.Reasons) : null,
                IsSuspicious = risk.IsSuspicious
            };

            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Kayıt atılamadıysa anahtarı serbest bırak, kullanıcı tekrar deneyebilsin
            await _idempotency.ReleaseAsync(userId, idempotencyKey);
            throw;
        }

        // 4) Account Service'ten parayı taşımasını iste
        try
        {
            var result = await _accountClient.TransferAsync(new InternalTransferRequest(
                userId,
                request.SourceAccountId,
                transaction.TargetAccountNumber,
                request.Amount,
                transaction.Id));

            transaction.Status = TransactionStatus.Completed;
            transaction.Currency = result.Currency;
            transaction.SourceAccountNumber = result.SourceAccountNumber;
            transaction.TargetAccountId = result.TargetAccountId;
            transaction.TargetAccountNumber = result.TargetAccountNumber;
            transaction.ReceiverUserId = result.ReceiverUserId;
            transaction.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await PublishCompletedEventAsync(transaction);
            return ToResponse(transaction, isDuplicate: false);
        }
        catch (AppException ex)
        {
            // Yetersiz bakiye, dondurulmuş hesap vb.: işlemi "Failed" olarak kaydet (admin panelinde görünecek)
            transaction.Status = TransactionStatus.Failed;
            transaction.FailureReason = ex.Message;
            await _db.SaveChangesAsync();
            throw;
        }
    }
    private async Task<RiskResult> EvaluateRiskAsync(Guid userId, decimal amount, string targetAccountNumber)
    {
        var since = DateTime.UtcNow.AddMinutes(-RiskCalculator.VelocityWindowMinutes);

        var recentCount = await _db.Transactions
            .CountAsync(t => t.SenderUserId == userId && t.CreatedAt >= since);

        var knownReceiver = await _db.Transactions.AnyAsync(t =>
            t.SenderUserId == userId &&
            t.TargetAccountNumber == targetAccountNumber &&
            t.Status == TransactionStatus.Completed);

        return RiskCalculator.Calculate(new RiskContext(amount, recentCount, IsNewReceiver: !knownReceiver));
    }
    private async Task PublishCompletedEventAsync(BankTransaction t)
    {
        try
        {
            await _publisher.PublishAsync(
                new TransactionCompletedEvent(
                    t.Id,
                    t.SenderUserId,
                    t.ReceiverUserId ?? Guid.Empty,
                    t.Amount,
                    t.Currency ?? "TRY",
                    t.Description ?? string.Empty),
                MessagingConstants.TransactionCompletedRoutingKey);
        }
        catch (Exception ex)
        {
            // Para zaten taşındı. RabbitMQ'ya ulaşılamadı diye kullanıcıya hata dönmeyiz, sadece logluyoruz.
            _logger.LogWarning(ex, "Could not publish TransactionCompletedEvent for transaction {TransactionId}", t.Id);
        }
    }

    public async Task<TransferResponse> GetByIdAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _db.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId
                && (t.SenderUserId == userId || t.ReceiverUserId == userId));

        if (transaction is null)
            throw new NotFoundException("Transaction not found.");

        return ToResponse(transaction, isDuplicate: false);
    }

    private static TransferResponse ToResponse(BankTransaction t, bool isDuplicate) => new(
        t.Id,
        t.Status.ToString(),
        t.Amount,
        t.Currency,
        t.SourceAccountNumber,
        t.TargetAccountNumber,
        t.Description,
        t.Category,
        t.FailureReason,
        t.CreatedAt,
        isDuplicate);
}