using FinCore.Notification.Application.Abstractions;
using FinCore.Notification.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace FinCore.Notification.Api.Hubs;

public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task PushAsync(Guid userId, NotificationResponse notification) =>
        _hub.Clients.Group(userId.ToString()).SendAsync("notificationReceived", notification);
}