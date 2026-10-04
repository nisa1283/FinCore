namespace FinCore.Notification.Application.DTOs;

public record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid? ReferenceId,
    bool IsRead,
    DateTime CreatedAt);