namespace Legi.Messaging.RabbitMq;

/// <summary>
/// Naming conventions for RabbitMQ exchanges and queues. Centralized here so
/// producer and consumer agree on names without coordination through code.
/// 
/// Exchange per integration event type (fanout, durable):
///   legi.events.{type-fqn-lowercase}
/// 
/// Queue per (service, event) pair (durable):
///   {service}.{event-name-kebab}
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.4.
/// </summary>
public static class RabbitMqTopology
{
    /// <summary>
    /// Returns the exchange name for an integration event type. Same name on
    /// the producer and all consumer sides — derivable from the type alone.
    /// </summary>
    public static string ExchangeNameFor(Type eventType)
    {
        if (eventType is null)
            throw new ArgumentNullException(nameof(eventType));

        var fullName = eventType.FullName
            ?? throw new InvalidOperationException(
                $"Type '{eventType.Name}' has no full name. " +
                "This is unexpected for an integration event.");

        return $"legi.events.{fullName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Returns the queue name for a (service, event type) pair. Each service
    /// that consumes an event gets its own queue bound to the event's exchange,
    /// so multiple services receive copies independently.
    /// </summary>
    public static string QueueNameFor(string serviceName, Type eventType)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name is required.", nameof(serviceName));
        if (eventType is null)
            throw new ArgumentNullException(nameof(eventType));

        var eventName = ToKebabCase(StripIntegrationEventSuffix(eventType.Name));
        return $"{serviceName.ToLowerInvariant()}.{eventName}";
    }

    /// <summary>
    /// Per-service <b>direct</b> exchange that carries a message both ways through
    /// the retry cycle: work queue → (dead-letter) → retry queue, and retry queue
    /// → (TTL expiry, dead-letter) → work queue. Routing keys are the queue names
    /// themselves, so a fanout work exchange's retries never fan back out to other
    /// services' queues. See Fase 6 (6A).
    /// </summary>
    public static string RetryExchangeNameFor(string serviceName)
    {
        RequireServiceName(serviceName);
        return $"legi.retry.{serviceName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Per-service <b>direct</b> exchange for the parking lot (terminal error
    /// queue). Messages that exhaust their retry budget are published here.
    /// </summary>
    public static string ParkingExchangeNameFor(string serviceName)
    {
        RequireServiceName(serviceName);
        return $"legi.parking.{serviceName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Wait/retry queue for a (service, event) pair: holds the message for a TTL,
    /// then dead-letters it back to the work queue. Name = work queue + ".retry".
    /// </summary>
    public static string RetryQueueNameFor(string serviceName, Type eventType)
        => $"{QueueNameFor(serviceName, eventType)}.retry";

    /// <summary>
    /// Parking-lot (error) queue for a (service, event) pair — no consumer.
    /// Name = work queue + ".error".
    /// </summary>
    public static string ErrorQueueNameFor(string serviceName, Type eventType)
        => $"{QueueNameFor(serviceName, eventType)}.error";

    private static void RequireServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name is required.", nameof(serviceName));
    }

    private static string StripIntegrationEventSuffix(string name)
    {
        const string suffix = "IntegrationEvent";
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name[..^suffix.Length]
            : name;
    }

    private static string ToKebabCase(string pascalCase)
    {
        // "UserBookRated" -> "user-book-rated"
        // "Ping" -> "ping"
        if (string.IsNullOrEmpty(pascalCase))
            return pascalCase;

        var result = new System.Text.StringBuilder(pascalCase.Length + 4);
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var ch = pascalCase[i];
            if (i > 0 && char.IsUpper(ch))
                result.Append('-');
            result.Append(char.ToLowerInvariant(ch));
        }
        return result.ToString();
    }
}