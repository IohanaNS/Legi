namespace Legi.Messaging.Outbox;

/// <summary>
/// A pending integration event in the transactional outbox. Written to the
/// service's database in the SAME transaction as the domain changes that
/// produced it. The <see cref="OutboxDispatcherWorker{TContext}"/> later picks
/// it up, publishes it to RabbitMQ, and marks it processed.
/// 
/// Fields are mutable to allow the dispatcher to update <see cref="ProcessedAt"/>,
/// <see cref="Attempts"/>, and <see cref="Error"/>; everything else is set at
/// construction and never changed.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.5.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for this message. Used as the broker's MessageId,
    /// allowing the consumer to deduplicate via the inbox table.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Assembly-qualified name of the integration event type. Used by the
    /// consumer to deserialize the payload into a concrete .NET type.
    /// </summary>
    public string Type { get; init; } = null!;

    /// <summary>
    /// JSON-serialized event payload (System.Text.Json).
    /// </summary>
    public string Payload { get; init; } = null!;

    /// <summary>
    /// Timestamp at which the producer published the event (UTC). Used by the
    /// dispatcher for FIFO ordering of pending messages.
    /// </summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Timestamp at which the broker confirmed receipt (UTC). Null while
    /// pending; non-null once successfully published.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of publishing attempts so far. Incremented on each failure. When
    /// it reaches OutboxOptions.MaxAttempts, the row is marked poison and
    /// skipped on subsequent dispatcher runs.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Last failure message, if any. Useful for diagnosing poison rows.
    /// Null on success.
    /// </summary>
    public string? Error { get; set; }
}