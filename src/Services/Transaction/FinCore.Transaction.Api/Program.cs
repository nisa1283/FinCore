using FinCore.BuildingBlocks.Middleware;
using FinCore.BuildingBlocks.Security;
using FinCore.Transaction.Application;
using FinCore.Transaction.Infrastructure;
using FinCore.BuildingBlocks.Messaging;
using FinCore.Transaction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FinCore.BuildingBlocks.Observability;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddFinCoreLogging("Transaction");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("TransactionDb")!,
    builder.Configuration["Services:AccountApi"]!,
    builder.Configuration["InternalApi:Key"]
        ?? throw new InvalidOperationException("InternalApi:Key bulunamadý. 'dotnet user-secrets' ile ayarla."),
    builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddFinCoreCors(builder.Configuration);
builder.Services.AddRabbitMqPublisher(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt("FinCore Transaction API");

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

app.MapGet("/health", () => Results.Ok(new { service = "Transaction", status = "healthy" }));
app.MapControllers();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TransactionDbContext>().Database.MigrateAsync();
}
app.Run();