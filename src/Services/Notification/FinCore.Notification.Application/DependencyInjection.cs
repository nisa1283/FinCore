using FinCore.Notification.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}