using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Legi.Messaging.RabbitMq;

/// <summary>
/// <see cref="IRabbitMqPublisher"/> implementation using RabbitMQ.Client 7.x.
/// 
/// Operational behavior:
/// <list type="bullet">
///   <item>One channel created per publish (cheap; not pooled in v1)</item>
///   <item>Publisher confirms enabled with tracking — PublishAsync awaits the broker ACK</item>
///   <item>Messages are persistent (DeliveryMode = 2)</item>
///   <item>Exchange declared idempotently on first use, cached per process</item>
///   <item>Type name resolved from the typeName string, not from a generic parameter,
///         because the dispatcher reads serialized rows whose Type is a string</item>
/// </list>
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, sections 2.3 and 7.4.
/// </summary>
public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPublisher> _logger;

    // Tracks which exchanges have been declared in this process so we don't
    // re-declare on every publish. Concurrent because publishes can race.
    private readonly ConcurrentDictionary<string, byte> _declaredExchanges = new();

    public RabbitMqPublisher(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishAsync(
        string typeName,
        string payload,
        Guid messageId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default)
    {
        var eventType = Type.GetType(typeName, throwOnError: false)
            ?? throw new InvalidOperationException(
                $"Cannot resolve type '{typeName}' for publishing. " +
                "The type may have been renamed or removed since the message was produced.");

        var exchange = RabbitMqTopology.ExchangeNameFor(eventType);

        var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        // Channel-per-publish keeps lifetime trivial. Publisher confirms with
        // tracking turned on means BasicPublishAsync awaits the broker ACK.
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel = await connection.CreateChannelAsync(
            channelOptions,
            cancellationToken);

        await EnsureExchangeDeclaredAsync(channel, exchange, cancellationToken);

        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            MessageId = messageId.ToString(),
            Type = typeName,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(occurredAt, TimeSpan.Zero).ToUnixTimeSeconds()),
        };

        // Fanout exchange — routing key is ignored, pass empty.
        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published message {MessageId} of type {Type} to exchange {Exchange}",
            messageId, typeName, exchange);
    }

    private async Task EnsureExchangeDeclaredAsync(
        IChannel channel,
        string exchange,
        CancellationToken cancellationToken)
    {
        if (_declaredExchanges.ContainsKey(exchange))
            return;

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        _declaredExchanges.TryAdd(exchange, 0);
    }
}