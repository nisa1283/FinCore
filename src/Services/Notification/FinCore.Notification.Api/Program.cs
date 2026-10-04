using FinCore.BuildingBlocks.Middleware;
using FinCore.BuildingBlocks.Security;
using FinCore.Notification.Api.Hubs;
using FinCore.Notification.Application;
using FinCore.Notification.Application.Abstractions;
using FinCore.Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("NotificationDb")!,
    builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationPusher, SignalRNotificationPusher>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt("FinCore Notification API");

var app = builder.Build();

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();   // wwwroot içindeki SignalR test sayfasý için

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { service = "Notification", status = "healthy" }));
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();