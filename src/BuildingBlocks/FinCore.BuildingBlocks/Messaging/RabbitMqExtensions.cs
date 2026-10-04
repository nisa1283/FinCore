using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.BuildingBlocks.Messaging;

public static class RabbitMqExtensions
{
    // Sadece ayarları okur (consumer tarafı bunu kullanır)
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        return services;
    }

    // Ayarlar + event yayınlayıcı (publisher tarafı bunu kullanır)
    public static IServiceCollection AddRabbitMqPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMq(configuration);
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        return services;
    }
}