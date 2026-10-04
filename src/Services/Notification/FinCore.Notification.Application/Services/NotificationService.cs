using System.Globalization;
using FinCore.BuildingBlocks.Events;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.BuildingBlocks.Responses;
using FinCore.Notification.Application.Abstractions;
using FinCore.Notification.Application.DTOs;
using FinCore.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinCore.Notification.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationDbContext _db;
    private readonly INotificationPusher _pusher;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationDbContext db,
        INotificationPusher pusher,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _pusher = pusher;
        _logger = logger;
    }

    public async Task HandleTransactionCompletedAsync(TransactionCompletedEvent e)
    {
        var amount = $"{e.Amount.ToString("N2", CultureInfo.InvariantCulture)} {e.Currency}";
        var note = string.IsNullOrWhiteSpace(e.Description) ? "" : $" Note: {e.Description}";

        var created = new List<UserNotification>();

        await AddIfNewAsync(created, e.EventId, e.SenderUserId,
            "TransferSent", "Transfer sent", $"You sent {amount}.{note}", e.TransactionId);

        if (e.ReceiverUserId != Guid.Empty)
        {
            await AddIfNewAsync(created, e.EventId, e.ReceiverUserId,
                "TransferReceived", "Money received", $"You received {amount}.{note}", e.TransactionId);
        }

        if (created.Count == 0)
            return; // bu event daha önce işlenmiş

        await _db.SaveChangesAsync();

        // Önce kaydet, sonra anlık gönder. Kullanıcı çevrimdışıysa bildirim yine DB'de durur.
        foreach (var notification in created)
        {
            try
            {
                await _pusher.PushAsync(notification.UserId, ToResponse(notification));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Real-time push failed for notification {NotificationId}", notification.Id);
            }
        }
    }

    public async Task<PagedResult<NotificationResponse>> GetMineAsync(Guid userId, int page, int pageSize, bool unreadOnly)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<NotificationResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public Task<int> GetUnreadCountAsync(Guid userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
            ?? throw new NotFoundException("Notification not found.");

        notification.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
            notification.IsRead = true;

        await _db.SaveChangesAsync();
    }

    private async Task AddIfNewAsync(
        List<UserNotification> created, Guid eventId, Guid userId,
        string type, string title, string message, Guid referenceId)
    {
        // RabbitMQ "en az bir kez" teslim eder, aynı mesaj bazen iki kez gelebilir
        var exists = await _db.Notifications
            .AnyAsync(n => n.EventId == eventId && n.UserId == userId && n.Type == type);

        if (exists)
            return;

        var notification = new UserNotification
        {
            UserId = userId,
            EventId = eventId,
            Type = type,
            Title = title,
            Message = message,
            ReferenceId = referenceId
        };

        _db.Notifications.Add(notification);
        created.Add(notification);
    }

    private static NotificationResponse ToResponse(UserNotification n) =>
        new(n.Id, n.Type, n.Title, n.Message, n.ReferenceId, n.IsRead, n.CreatedAt);
}