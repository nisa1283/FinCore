using FinCore.BuildingBlocks.Events;
using FinCore.BuildingBlocks.Responses;
using FinCore.Notification.Application.DTOs;

namespace FinCore.Notification.Application.Services;

public interface INotificationService
{
    Task HandleTransactionCompletedAsync(TransactionCompletedEvent @event);
    Task<PagedResult<NotificationResponse>> GetMineAsync(Guid userId, int page, int pageSize, bool unreadOnly);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid userId, Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}