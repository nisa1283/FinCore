using FinCore.BuildingBlocks.Contracts;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Transaction.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionDbContext _db;
    private readonly IAccountClient _accountClient;
    private readonly IIdempotencyService _idempotency;
    private readonly IValidator<TransferRequest> _validator;

    public TransactionService(
        ITransactionDbContext db,
        IAccountClient accountClient,
        IIdempotencyService idempotency,
        IValidator<TransferRequest> validator)
    {
        _db = db;
        _accountClient = accountClient;
        _idempotency = idempotency;
        _validator = validator;
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

        // 3) İşlemi "Pending" olarak kaydet
        var transaction = new BankTransaction
        {
            IdempotencyKey = idempotencyKey,
            SenderUserId = userId,
            SourceAccountId = request.SourceAccountId,
            TargetAccountNumber = request.TargetAccountNumber.Trim().ToUpperInvariant(),
            Amount = request.Amount,
            Description = request.Description?.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category
        };

        try
        {
            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Kayıt bile atılamadıysa anahtarı serbest bırak, kullanıcı tekrar deneyebilsin
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