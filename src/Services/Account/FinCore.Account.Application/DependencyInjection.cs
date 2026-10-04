using FinCore.Account.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.Account.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IAccountService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IFundsTransferService, FundsTransferService>();
        services.AddScoped<IAdminAccountService, AdminAccountService>();
        return services;
    }
}