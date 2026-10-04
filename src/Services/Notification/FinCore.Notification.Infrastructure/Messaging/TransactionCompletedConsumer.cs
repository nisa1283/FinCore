using System.Text.Json;
using FinCore.BuildingBlocks.Events;
using FinCore.BuildingBlocks.Messaging;
using FinCore.Notification.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FinCore.Notification.Infrastructure.Messaging;

public class TransactionCompletedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionCompletedConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public TransactionCompletedConsumer(
        IOptions<RabbitMqSettings> options,
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionCompletedConsumer> logger)
    {
        _settings = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ henüz hazır değilse (container yeni açıldıysa) pes etme, tekrar dene
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsuming();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not connect to RabbitMQ. Retrying in 5 seconds...");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private void StartConsuming()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            AutomaticRecoveryEnabled = true,
            DispatchConsumersAsync = true   // async consumer kullanacağımız için şart
        };

        _connection = factory.CreateConnection("fincore-notification-consumer");
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(MessagingConstants.ExchangeName, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(MessagingConstants.NotificationQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(
            MessagingConstants.NotificationQueueName,
            MessagingConstants.ExchangeName,
            MessagingConstants.TransactionCompletedRoutingKey);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        // autoAck: false → mesajı biz işleyip onaylayana kadar RabbitMQ silmez
        _channel.BasicConsume(MessagingConstants.NotificationQueueName, autoAck: false, consumer);

        _logger.LogInformation("Listening on queue {Queue}", MessagingConstants.NotificationQueueName);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = ((AsyncEventingBasicConsumer)sender).Model;

        try
        {
            var @event = JsonSerializer.Deserialize<TransactionCompletedEvent>(ea.Body.Span, JsonOptions);

            if (@event is not null)
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await service.HandleTransactionCompletedAsync(@event);
            }

            channel.BasicAck(ea.DeliveryTag, multiple: false);   // "işledim, silebilirsin"
        }
        catch (JsonException ex)
        {
            // Bozuk mesaj bir daha okunsa da düzelmez, kuyruğa geri koyma
            _logger.LogError(ex, "Invalid message received, discarding.");
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            // Geçici hata olabilir (örn. DB kapalı), mesajı kuyruğa geri koy
            _logger.LogError(ex, "Failed to process message, requeueing.");
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}