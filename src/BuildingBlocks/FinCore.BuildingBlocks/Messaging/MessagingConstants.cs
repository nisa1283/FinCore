namespace FinCore.BuildingBlocks.Messaging;

public static class MessagingConstants
{
    public const string ExchangeName = "fincore.events";
    public const string TransactionCompletedRoutingKey = "transaction.completed";
    public const string NotificationQueueName = "notification.transaction-completed";
}