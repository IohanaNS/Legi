using System.Text;
using Legi.Contracts;
using Legi.Messaging.Inbox;
using Legi.Messaging.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Legi.Messaging.RabbitMq;

/// <summary>
/// Background service that consumes a single integration event type from
/// RabbitMQ and dispatches it through the consumer pipeline. One instance
/// per (service, event type) pair.
/// 
/// Lifecycle:
/// <list type="number">
///   <item>Open a channel scoped to the host's lifetime</item>
///   <item>Declare the exchange (idempotent), this consumer's queue, and
///         the binding</item>
///   <item>Set prefetch (BasicQos)</item>
///   <item>Register an async consumer; the receive callback dispatches via
///         <see cref="IntegrationEventDispatcher{TContext}"/></item>
///   <item>On any failure in the callback: nack with requeue=true so the
///         broker redelivers later. Never let an exception escape the
///         callback — that leaves the message unacked indefinitely</item>
/// </list>
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, sections 3.3 and 7.4.
/// </summary>
public class RabbitMqConsumerHost<TEvent, TContext> : BackgroundService
    where TEvent : class, IIntegrationEvent
    where TContext : DbContext
{
    private const ushort PrefetchCount = 10;

    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IntegrationEventDispatcher<TContext> _dispatcher;
    private readonly IntegrationEventSerializer _serializer;
    private readonly MessagingHostingOptions _hostingOptions;
    private readonly ILogger<RabbitMqConsumerHost<TEvent, TContext>> _logger;

    private IChannel? _channel;

    public RabbitMqConsumerHost(
        RabbitMqConnectionFactory connectionFactory,
        IntegrationEventDispatcher<TContext> dispatcher,
        IntegrationEventSerializer serializer,
        IOptions<MessagingHostingOptions> hostingOptions,
        ILogger<RabbitMqConsumerHost<TEvent, TContext>> logger)
    {
        _connectionFactory = connectionFactory;
        _dispatcher = dispatcher;
        _serializer = serializer;
        _hostingOptions = hostingOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var eventType = typeof(TEvent);
        var exchange = RabbitMqTopology.ExchangeNameFor(eventType);
        var queue = RabbitMqTopology.QueueNameFor(_hostingOptions.ServiceName, eventType);

        _logger.LogInformation(
            "Starting consumer host for {EventType}; queue: {Queue}, exchange: {Exchange}",
            eventType.Name, queue, exchange);

        var connection = await _connectionFactory.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declare topology. ExchangeDeclareAsync on a pre-existing exchange
        // is a no-op verification, so this is safe to call from every consumer.
        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: queue,
            exchange: exchange,
            routingKey: string.Empty,
            arguments: null,
            cancellationToken: stoppingToken);

        // Prefetch limits unacked messages in flight on this channel. Without
        // this, RabbitMQ pushes as fast as it can; a slow handler would let
        // memory balloon.
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Consumer host ready for {EventType}; consuming from {Queue}",
            eventType.Name, queue);

        // Keep the host alive until shutdown. The actual work happens in the
        // consumer callback, driven by the broker.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }

        _logger.LogInformation("Consumer host stopped for {EventType}", eventType.Name);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        // The channel guarantees one OnReceivedAsync at a time per consumer
        // (they are dispatched sequentially), so we do not worry about
        // concurrent invocations interleaving.

        if (_channel is null)
        {
            // Defensive: should not happen, but if a delivery arrives before
            // the channel was assigned (shouldn't), we cannot ack — bail out
            // and let the connection close eventually.
            _logger.LogError("Received message but channel is null");
            return;
        }

        var deliveryTag = ea.DeliveryTag;
        var typeName = ea.BasicProperties.Type ?? typeof(TEvent).AssemblyQualifiedName!;
        Guid messageId;

        if (!Guid.TryParse(ea.BasicProperties.MessageId, out messageId))
        {
            // Malformed envelope — no MessageId means we cannot dedup.
            // Drop without requeue: this is permanent garbage.
            _logger.LogError(
                "Received message with missing or invalid MessageId on queue for {EventType}; discarding",
                typeof(TEvent).Name);
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(ea.Body.Span);
            var deserialized = _serializer.Deserialize(typeName, payload);

            await _dispatcher.DispatchAsync(messageId, typeName, deserialized, ea.CancellationToken);

            await _channel.BasicAckAsync(deliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // Any failure in the pipeline — deserialization, handler exception,
            // database error — results in nack-with-requeue. The broker
            // redelivers; if the failure is transient, the next attempt
            // succeeds. If permanent, we get a visible redelivery loop in logs.
            // See decision 8.2 for the v1 retry policy rationale.
            _logger.LogError(ex,
                "Failed to process message {MessageId} of type {EventType}; nack with requeue",
                messageId, typeof(TEvent).Name);

            try
            {
                await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx,
                    "Failed to nack message {MessageId}; the broker will redeliver after channel close",
                    messageId);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
            _channel = null;
        }
    }
}