namespace FinCore.Notification.Domain.Entities;

public class UserNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }          // Aynı event iki kez gelirse tekrar bildirim oluşturmamak için
    public string Type { get; set; } = string.Empty;   // TransferSent, TransferReceived
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }     // ilgili işlemin Id'si
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}