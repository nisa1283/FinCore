using FinCore.BuildingBlocks.Messaging;
using FinCore.Notification.Application.Abstractions;
using FinCore.Notification.Infrastructure.Messaging;
using FinCore.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());

        services.AddRabbitMq(configuration);
        services.AddHostedService<TransactionCompletedConsumer>();

        return services;
    }
}