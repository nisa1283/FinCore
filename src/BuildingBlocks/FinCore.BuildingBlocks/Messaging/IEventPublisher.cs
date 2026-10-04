using FinCore.BuildingBlocks.Events;

namespace FinCore.BuildingBlocks.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey) where T : IntegrationEvent;
}