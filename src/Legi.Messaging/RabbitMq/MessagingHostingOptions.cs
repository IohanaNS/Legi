namespace Legi.Messaging.RabbitMq;

/// <summary>
/// Per-service configuration that is set at registration time (not from
/// configuration files). Currently, it carries the service's lowercase name
/// used when constructing queue names.
/// 
/// See <see cref="RabbitMqTopology.QueueNameFor"/> and the
/// <c>AddLegiMessaging</c> registration extension.
/// </summary>
public class MessagingHostingOptions
{
    /// <summary>
    /// Lowercase short name of the service that hosts this messaging
    /// infrastructure (e.g., "identity", "library", "catalog", "social").
    /// Used as the prefix for RabbitMQ queue names.
    /// </summary>
    public string ServiceName { get; set; } = null!;

    /// <summary>
    /// Time (ms) a message waits in the retry queue before being redelivered to
    /// the work queue. Single fixed TTL (flat backoff) — a documented Fase 6
    /// simplification over staged/exponential retry queues.
    /// </summary>
    public int RetryTtlMs { get; set; } = 30_000;

    /// <summary>
    /// Max processing attempts for a <b>generic</b> (probable-poison) failure
    /// before the message is parked. Kept low so a real bug surfaces fast instead
    /// of spinning thousands of redeliveries.
    /// </summary>
    public int MaxConsumerAttempts { get; set; } = 5;

    /// <summary>
    /// Max attempts for a <see cref="Legi.SharedKernel.TransientMessagingException"/>
    /// (self-resolving condition, §8.3) before parking. Generous: these
    /// <i>should</i> keep retrying until the prerequisite event arrives. A still-
    /// failing "transient" after this many tries is treated as stuck and parked.
    /// </summary>
    public int MaxTransientAttempts { get; set; } = 50;

    /// <summary>
    /// Unprocessed outbox rows above which the outbox-backlog health check reports
    /// <c>Degraded</c> — the early warning that the dispatcher is stuck or the
    /// broker is down. The service stays Healthy below this.
    /// </summary>
    public int OutboxBacklogThreshold { get; set; } = 1_000;
}