namespace Legi.Messaging.Inbox;

/// <summary>
/// Record of an integration event that has been processed by a consumer.
/// Used by <see cref="IntegrationEventDispatcher"/> to detect and skip
/// duplicate deliveries from RabbitMQ — the inbox half of the
/// outbox-inbox pattern.
/// 
/// All fields are write-once. After the row is inserted, it is never
/// updated; only read for dedup checks (or eventually purged by a
/// retention job in Phase 6).
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 8.1 and section 7.5.
/// </summary>
public class InboxMessage
{
    /// <summary>
    /// MessageId received from the broker, originally produced by the
    /// publisher and persisted as the OutboxMessage.Id. Acts as the dedup
    /// key.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Assembly-qualified type name of the integration event. Stored for
    /// diagnostic purposes only — never used for dispatch (the consumer
    /// host already knows its event type at compile time).
    /// </summary>
    public string Type { get; init; } = null!;

    /// <summary>
    /// Timestamp at which the consumer's handler completed and the inbox
    /// row was committed (UTC).
    /// </summary>
    public DateTime ProcessedAt { get; init; }
}