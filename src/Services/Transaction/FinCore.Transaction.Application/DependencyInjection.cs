using FinCore.Transaction.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.Transaction.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ITransactionService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransactionQueryService, TransactionQueryService>();
        return services;
    }
}