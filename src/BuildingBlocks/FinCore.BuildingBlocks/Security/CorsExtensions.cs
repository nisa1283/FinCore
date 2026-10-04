using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.BuildingBlocks.Security;

public static class CorsExtensions
{
    public const string PolicyName = "FinCoreFrontend";

    public static IServiceCollection AddFinCoreCors(this IServiceCollection services, IConfiguration configuration)
    {
        // appsettings.json'da "Cors:AllowedOrigins" yoksa Vite'in varsayılan adresine izin ver
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };

        services.AddCors(options =>
            options.AddPolicy(PolicyName, policy =>
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()));   // SignalR için gerekli

        return services;
    }
}