using FinCore.Notification.Application.DTOs;

namespace FinCore.Notification.Application.Abstractions;

public interface INotificationPusher
{
    Task PushAsync(Guid userId, NotificationResponse notification);
}