using FinCore.Notification.Application.Abstractions;
using FinCore.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext, INotificationDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<UserNotification> Notifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserNotification>(e =>
        {
            e.ToTable("Notifications");
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(100).IsRequired();
            e.Property(x => x.Message).HasMaxLength(300).IsRequired();

            e.HasIndex(x => new { x.UserId, x.CreatedAt });

            // Aynı event, aynı kullanıcı ve aynı tipte ikinci bildirim oluşamaz
            e.HasIndex(x => new { x.EventId, x.UserId, x.Type }).IsUnique();
        });
    }
}