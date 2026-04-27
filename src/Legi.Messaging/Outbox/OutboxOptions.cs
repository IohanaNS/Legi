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
}