using System.Text.Json;
using FinCore.BuildingBlocks.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FinCore.BuildingBlocks.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConnectionFactory _factory;
    private readonly object _lock = new();
    private IConnection? _connection;

    public RabbitMqEventPublisher(IOptions<RabbitMqSettings> options)
    {
        var settings = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
        };
    }

    public Task PublishAsync<T>(T @event, string routingKey) where T : IntegrationEvent
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, JsonOptions);

        // Channel thread-safe değil, bu yüzden her yayın için kısa ömürlü bir tane açıyoruz
        using var channel = GetConnection().CreateModel();

        channel.ExchangeDeclare(MessagingConstants.ExchangeName, ExchangeType.Topic, durable: true);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;               // RabbitMQ yeniden başlasa mesaj kaybolmasın
        properties.ContentType = "application/json";
        properties.MessageId = @event.EventId.ToString();

        channel.BasicPublish(MessagingConstants.ExchangeName, routingKey, properties, body);

        return Task.CompletedTask;
    }

    private IConnection GetConnection()
    {
        lock (_lock)
        {
            if (_connection is null || !_connection.IsOpen)
            {
                _connection?.Dispose();
                _connection = _factory.CreateConnection("fincore-publisher");
            }

            return _connection;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}