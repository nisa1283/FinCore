using FinCore.Account.Application.Abstractions;
using FinCore.Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.Account.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AccountDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<AccountDbContext>());
        return services;
    }
}