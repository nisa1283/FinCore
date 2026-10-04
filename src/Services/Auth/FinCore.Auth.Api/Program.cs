using FinCore.Auth.Application;
using FinCore.Auth.Infrastructure;
using FinCore.BuildingBlocks.Middleware;
using FinCore.BuildingBlocks.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("AuthDb")!);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddFinCoreCors(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt("FinCore Auth API");

var app = builder.Build();

app.UseGlobalExceptionHandler();
app.UseCors(CorsExtensions.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // "Kimsin?" (token'ý okur)
app.UseAuthorization();  // "Buna yetkin var mý?"

app.MapGet("/health", () => Results.Ok(new { service = "Auth", status = "healthy" }));
app.MapControllers();

app.Run();