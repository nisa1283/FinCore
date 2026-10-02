using FinCore.Auth.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IAuthService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}