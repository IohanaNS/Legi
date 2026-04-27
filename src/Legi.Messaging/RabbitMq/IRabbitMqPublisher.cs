namespace Legi.Messaging.RabbitMq;

/// <summary>
/// Publishes an already-serialized integration event to the broker. Used by
/// <see cref="Outbox.OutboxDispatcherWorker{TContext}"/>; not called directly
/// by application code (which goes through <see cref="SharedKernel.IEventBus"/>).
/// 
/// Returns only after the broker confirms a durable receipt (publisher confirms).
/// Throws on NACK or timeout — the dispatcher catches and retries.
/// </summary>
public interface IRabbitMqPublisher
{
    Task PublishAsync(
        string typeName,
        string payload,
        Guid messageId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);
}