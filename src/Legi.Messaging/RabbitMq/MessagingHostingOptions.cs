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
    public string ServiceName { get; init; } = null!;
}