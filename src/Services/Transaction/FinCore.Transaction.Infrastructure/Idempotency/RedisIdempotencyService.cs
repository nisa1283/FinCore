using FinCore.Transaction.Application.Abstractions;
using StackExchange.Redis;

namespace FinCore.Transaction.Infrastructure.Idempotency;

public class RedisIdempotencyService : IIdempotencyService
{
    private static readonly TimeSpan KeyLifetime = TimeSpan.FromHours(24);

    private readonly IConnectionMultiplexer _redis;

    public RedisIdempotencyService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> TryAcquireAsync(Guid userId, string idempotencyKey)
    {
        var db = _redis.GetDatabase();
        return await db.StringSetAsync(BuildKey(userId, idempotencyKey), "1", KeyLifetime, When.NotExists);
    }

    public async Task ReleaseAsync(Guid userId, string idempotencyKey)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(BuildKey(userId, idempotencyKey));
    }

    private static string BuildKey(Guid userId, string idempotencyKey) =>
        $"idem:transfer:{userId}:{idempotencyKey}";
}