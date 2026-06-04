using System.Text;
using Legi.Contracts;
using Legi.Messaging.Diagnostics;
using Legi.Messaging.Inbox;
using Legi.Messaging.Serialization;
using Legi.SharedKernel;
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
///   <item>On failure: classify (transient §8.3 vs probable poison), then either
///         dead-letter into the retry queue (TTL-delayed redelivery) or, once the
///         retry budget is exhausted, divert to the parking-lot error queue. Never
///         requeue blindly (that loops poison forever) and never let an exception
///         escape the callback (that leaves the message unacked indefinitely)</item>
/// </list>
///
/// See MESSAGING-ARCHITECTURE-decisions.md, sections 3.3, 7.4, 8.2/8.3 and Fase 6 (6A/6B).
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
    private readonly MessagingMetrics _metrics;
    private readonly ILogger<RabbitMqConsumerHost<TEvent, TContext>> _logger;

    private IChannel? _channel;
    private string _parkingExchange = string.Empty;
    private string _errorQueue = string.Empty;

    public RabbitMqConsumerHost(
        RabbitMqConnectionFactory connectionFactory,
        IntegrationEventDispatcher<TContext> dispatcher,
        IntegrationEventSerializer serializer,
        IOptions<MessagingHostingOptions> hostingOptions,
        MessagingMetrics metrics,
        ILogger<RabbitMqConsumerHost<TEvent, TContext>> logger)
    {
        _connectionFactory = connectionFactory;
        _dispatcher = dispatcher;
        _serializer = serializer;
        _hostingOptions = hostingOptions.Value;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var eventType = typeof(TEvent);
        var service = _hostingOptions.ServiceName;
        var exchange = RabbitMqTopology.ExchangeNameFor(eventType);
        var queue = RabbitMqTopology.QueueNameFor(service, eventType);
        var retryExchange = RabbitMqTopology.RetryExchangeNameFor(service);
        var retryQueue = RabbitMqTopology.RetryQueueNameFor(service, eventType);
        _parkingExchange = RabbitMqTopology.ParkingExchangeNameFor(service);
        _errorQueue = RabbitMqTopology.ErrorQueueNameFor(service, eventType);

        _logger.LogInformation(
            "Starting consumer host for {EventType}; queue: {Queue}, exchange: {Exchange}",
            eventType.Name, queue, exchange);

        var connection = await _connectionFactory.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // --- Work exchange (fanout): the published-to exchange; one per event type ---
        await _channel.ExchangeDeclareAsync(
            exchange: exchange, type: ExchangeType.Fanout,
            durable: true, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        // --- Retry + parking exchanges (direct, per service). The retry exchange
        // carries both legs of the cycle (work→retry and retry→work) keyed by queue
        // name, so a fanout work exchange's retries never fan back out (Fase 6 6A). ---
        await _channel.ExchangeDeclareAsync(
            exchange: retryExchange, type: ExchangeType.Direct,
            durable: true, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync(
            exchange: _parkingExchange, type: ExchangeType.Direct,
            durable: true, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        // --- Work queue: dead-letters (on nack-without-requeue) to the retry queue. ---
        // NOTE: adding these args changes the queue declaration; a pre-existing queue
        // declared without them will fail with PRECONDITION_FAILED — delete the old
        // queues when migrating to this topology (Fase 6 runbook).
        await _channel.QueueDeclareAsync(
            queue: queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = retryExchange,
                ["x-dead-letter-routing-key"] = retryQueue
            },
            cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(
            queue: queue, exchange: exchange, routingKey: string.Empty,
            arguments: null, cancellationToken: stoppingToken);
        // Re-entry leg: the retry queue dead-letters back here, keyed by the work queue name.
        await _channel.QueueBindAsync(
            queue: queue, exchange: retryExchange, routingKey: queue,
            arguments: null, cancellationToken: stoppingToken);

        // --- Retry/wait queue: holds the message for a fixed TTL, then dead-letters
        // it back to the work queue (flat backoff). ---
        await _channel.QueueDeclareAsync(
            queue: retryQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = _hostingOptions.RetryTtlMs,
                ["x-dead-letter-exchange"] = retryExchange,
                ["x-dead-letter-routing-key"] = queue
            },
            cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(
            queue: retryQueue, exchange: retryExchange, routingKey: retryQueue,
            arguments: null, cancellationToken: stoppingToken);

        // --- Parking lot (error) queue: terminal, no consumer. ---
        await _channel.QueueDeclareAsync(
            queue: _errorQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(
            queue: _errorQueue, exchange: _parkingExchange, routingKey: _errorQueue,
            arguments: null, cancellationToken: stoppingToken);

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
        var eventName = typeof(TEvent).Name;
        var typeName = ea.BasicProperties.Type ?? typeof(TEvent).AssemblyQualifiedName!;

        if (!Guid.TryParse(ea.BasicProperties.MessageId, out var messageId))
        {
            // Malformed envelope — no MessageId means we cannot dedup. Park it
            // (not a silent drop, and not the retry loop — it will never improve).
            _logger.LogError(
                "Received message with missing or invalid MessageId on queue for {EventType}; parking",
                eventName);
            _metrics.RecordParked(eventName);
            await ParkAsync(ea, "missing or invalid MessageId");
            return;
        }

        // How many times this message was already rejected from the work queue
        // (0 on first delivery). Drives both the retry budget and the redelivery metric.
        var priorAttempts = ConsumerRetryPolicy.GetRejectedDeathCount(ea.BasicProperties.Headers);
        if (priorAttempts > 0)
            _metrics.RecordRedelivered(eventName);

        // Correlation scope (6C): every log line from the dispatcher and handlers
        // during this delivery carries the MessageId and event type. Message-template
        // form so the simple console formatter renders the values (with IncludeScopes)
        // and structured sinks capture them as named fields.
        using var scope = _logger.BeginScope(
            "MessageId:{MessageId} MessageType:{MessageType}", messageId, eventName);

        try
        {
            var payload = Encoding.UTF8.GetString(ea.Body.Span);
            var deserialized = _serializer.Deserialize(typeName, payload);

            await _dispatcher.DispatchAsync(messageId, typeName, deserialized, ea.CancellationToken);

            await _channel.BasicAckAsync(deliveryTag, multiple: false);
            _metrics.RecordConsumed(eventName);
        }
        catch (Exception ex)
        {
            _metrics.RecordFailed(eventName);

            // Classify (6B): a TransientMessagingException is a self-resolving
            // condition (§8.3) and gets a generous retry budget; anything else is
            // probable poison and parks fast. The attempt count comes from the
            // RabbitMQ x-death header (incremented each work→retry→work cycle).
            var isTransient = ex is TransientMessagingException;
            var decision = ConsumerRetryPolicy.Decide(
                isTransient, priorAttempts,
                _hostingOptions.MaxConsumerAttempts, _hostingOptions.MaxTransientAttempts);

            if (decision == RetryDecision.Park)
            {
                _logger.LogError(ex,
                    "Message {MessageId} ({EventType}) exhausted its retry budget after {Attempts} attempt(s) " +
                    "(transient={Transient}); parking",
                    messageId, eventName, priorAttempts + 1, isTransient);
                _metrics.RecordParked(eventName);
                await ParkAsync(ea, ex.Message);
                return;
            }

            if (isTransient)
                _logger.LogWarning(ex,
                    "Transient failure on {MessageId} ({EventType}), attempt {Attempt}; retrying in {Ttl}ms",
                    messageId, eventName, priorAttempts + 1, _hostingOptions.RetryTtlMs);
            else
                _logger.LogError(ex,
                    "Failure on {MessageId} ({EventType}), attempt {Attempt}/{Max}; retrying in {Ttl}ms",
                    messageId, eventName, priorAttempts + 1,
                    _hostingOptions.MaxConsumerAttempts, _hostingOptions.RetryTtlMs);

            await NackToRetryAsync(deliveryTag, messageId);
        }
    }

    /// <summary>
    /// nack without requeue → the work queue's dead-letter-exchange routes the
    /// message into the retry queue, where it waits the TTL and is then redelivered.
    /// </summary>
    private async Task NackToRetryAsync(ulong deliveryTag, Guid messageId)
    {
        try
        {
            await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
        catch (Exception nackEx)
        {
            _logger.LogError(nackEx,
                "Failed to nack message {MessageId} into the retry path; the broker will redeliver after channel close",
                messageId);
        }
    }

    /// <summary>
    /// Diverts a message to the parking-lot (error) queue and acks the original so
    /// it leaves the work queue. Parking publishes — it does NOT write the inbox
    /// row — so a manual re-drive from the error queue still processes exactly once.
    /// If the publish fails, the message is nacked into the retry path instead of
    /// being acked, so it is never silently lost.
    /// </summary>
    private async Task ParkAsync(BasicDeliverEventArgs ea, string reason)
    {
        try
        {
            var headers = new Dictionary<string, object?>(
                ea.BasicProperties.Headers ?? new Dictionary<string, object?>())
            {
                ["x-parked-reason"] = reason,
                ["x-parked-from"] = _channel is null ? string.Empty : _errorQueue
            };

            var props = new BasicProperties
            {
                MessageId = ea.BasicProperties.MessageId,
                Type = ea.BasicProperties.Type,
                Persistent = true,
                Headers = headers
            };

            await _channel!.BasicPublishAsync(
                exchange: _parkingExchange,
                routingKey: _errorQueue,
                mandatory: false,
                basicProperties: props,
                body: ea.Body);

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception parkEx)
        {
            _logger.LogError(parkEx,
                "Failed to park message {MessageId}; nacking into retry path instead",
                ea.BasicProperties.MessageId);
            await NackToRetryAsync(ea.DeliveryTag, Guid.Empty);
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