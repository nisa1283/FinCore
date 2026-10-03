using FinCore.BuildingBlocks.Security;
using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Infrastructure.Clients;
using FinCore.Transaction.Infrastructure.Idempotency;
using FinCore.Transaction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FinCore.Transaction.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string accountApiUrl,
        string internalApiKey,
        string redisConnection)
    {
        services.AddDbContext<TransactionDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ITransactionDbContext>(sp => sp.GetRequiredService<TransactionDbContext>());

        services.AddHttpClient<IAccountClient, AccountClient>(client =>
        {
            client.BaseAddress = new Uri(accountApiUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add(InternalApiKeyAttribute.HeaderName, internalApiKey);
        });

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IIdempotencyService, RedisIdempotencyService>();

        return services;
    }
}