using FinCore.Account.Application;
using FinCore.Account.Infrastructure;
using FinCore.BuildingBlocks.Middleware;
using FinCore.BuildingBlocks.Security;
using FinCore.Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FinCore.BuildingBlocks.Observability;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddFinCoreLogging("Account");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("AccountDb")!);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddFinCoreCors(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt("FinCore Account API");

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseGlobalExceptionHandler();
app.UseCors(CorsExtensions.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { service = "Account", status = "healthy" }));
app.MapControllers();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AccountDbContext>().Database.MigrateAsync();
}

app.Run();