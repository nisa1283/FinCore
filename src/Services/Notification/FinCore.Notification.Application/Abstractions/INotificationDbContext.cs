using FinCore.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Notification.Application.Abstractions;

public interface INotificationDbContext
{
    DbSet<UserNotification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}