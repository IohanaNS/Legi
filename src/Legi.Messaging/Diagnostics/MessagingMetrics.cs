using System.Diagnostics.Metrics;

namespace Legi.Messaging.Diagnostics;

/// <summary>
/// Process-wide messaging metrics (Fase 6 6C). A single <see cref="Meter"/> with
/// the consumer-side counters that matter for operating the system: throughput,
/// failure rate, and dead-letter/redelivery pressure. Registered as a singleton;
/// the meter is wired to the OTel metrics pipeline in <c>AddLegiMessaging</c>.
///
/// Outbox backlog/lag is surfaced via the outbox-backlog health check rather than
/// a metric here (it is the same query, and a degraded /health is the operator's
/// early warning). An observable outbox-pending gauge can be added later.
/// </summary>
public sealed class MessagingMetrics : IDisposable
{
    public const string MeterName = "Legi.Messaging";

    private readonly Meter _meter;
    private readonly Counter<long> _consumed;
    private readonly Counter<long> _failed;
    private readonly Counter<long> _parked;
    private readonly Counter<long> _redelivered;

    public MessagingMetrics()
    {
        _meter = new Meter(MeterName);
        _consumed = _meter.CreateCounter<long>(
            "legi.messaging.consumed", unit: "{message}",
            description: "Integration events processed and acked.");
        _failed = _meter.CreateCounter<long>(
            "legi.messaging.failed", unit: "{message}",
            description: "Integration event processing failures (each will retry or park).");
        _parked = _meter.CreateCounter<long>(
            "legi.messaging.parked", unit: "{message}",
            description: "Integration events diverted to the parking/error queue (terminal).");
        _redelivered = _meter.CreateCounter<long>(
            "legi.messaging.redelivered", unit: "{message}",
            description: "Integration event deliveries that had already been retried at least once.");
    }

    public void RecordConsumed(string eventType) => _consumed.Add(1, Tag(eventType));
    public void RecordFailed(string eventType) => _failed.Add(1, Tag(eventType));
    public void RecordParked(string eventType) => _parked.Add(1, Tag(eventType));
    public void RecordRedelivered(string eventType) => _redelivered.Add(1, Tag(eventType));

    private static KeyValuePair<string, object?> Tag(string eventType) => new("event", eventType);

    public void Dispose() => _meter.Dispose();
}
