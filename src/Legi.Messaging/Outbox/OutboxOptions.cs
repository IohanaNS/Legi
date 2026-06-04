namespace Legi.Messaging.Outbox;

/// <summary>
/// Configuration for the outbox dispatcher. Bound from the "Outbox" section of
/// the service's appsettings.json.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.3.
/// </summary>
public class OutboxOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Outbox";

    /// <summary>
    /// How often the dispatcher polls the outbox table for pending messages.
    /// Lower values reduce delivery latency but increase the database load.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of pending messages the dispatcher claims and publishes
    /// per polling cycle. Larger batches improve throughput; smaller batches
    /// reduce the blast radius of a stuck publication.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Maximum number of publishing attempts for a single message before it is
    /// marked poison and skipped on subsequent runs. The Error column records
    /// the last failure for manual diagnosis.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// How long processed outbox rows and consumed inbox rows are kept before the
    /// retention worker deletes them (Fase 6 6D.2). Poison outbox rows
    /// (<c>ProcessedAt == null</c>, attempts exhausted) are <b>never</b> deleted —
    /// they're kept for diagnosis. The inbox window must comfortably exceed the
    /// max realistic redelivery delay so dedup still works for late redeliveries.
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// How often the retention worker runs. Cleanup is cheap and not latency-
    /// sensitive, so this is coarse.
    /// </summary>
    public int RetentionIntervalMinutes { get; set; } = 60;
}