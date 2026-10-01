namespace FinCore.BuildingBlocks.Events;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record TransactionCompletedEvent(
    Guid TransactionId,
    Guid SenderUserId,
    Guid ReceiverUserId,
    decimal Amount,
    string Currency,
    string Description) : IntegrationEvent;