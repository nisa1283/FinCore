using FinCore.BuildingBlocks.Middleware;
using FinCore.BuildingBlocks.Security;
using FinCore.Transaction.Application;
using FinCore.Transaction.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("TransactionDb")!,
    builder.Configuration["Services:AccountApi"]!,
    builder.Configuration["InternalApi:Key"]
        ?? throw new InvalidOperationException("InternalApi:Key bulunamadý. 'dotnet user-secrets' ile ayarla."),
    builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt("FinCore Transaction API");

var app = builder.Build();

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { service = "Transaction", status = "healthy" }));
app.MapControllers();

app.Run();