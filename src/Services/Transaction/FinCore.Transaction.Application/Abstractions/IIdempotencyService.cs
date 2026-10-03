namespace FinCore.Transaction.Application.Abstractions;

public interface IIdempotencyService
{
    Task<bool> TryAcquireAsync(Guid userId, string idempotencyKey);
    Task ReleaseAsync(Guid userId, string idempotencyKey);
}