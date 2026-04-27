namespace Legi.Contracts.Diagnostics;

/// <summary>
/// Diagnostic event used to validate the messaging pipeline end-to-end.
/// Published and consumed by the same service (Identity) during the Phase 1
/// smoke test. Not used by any business flow.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, sub-phase 1I.
/// </summary>
/// <param name="PingId">Unique identifier for this specific ping, useful for
/// correlating the producer log with the consumer log.</param>
/// <param name="SentAt">Timestamp at which the ping was published, used to
/// measure end-to-end latency through outbox + RabbitMQ + inbox.</param>
public record PingIntegrationEvent(
    Guid PingId,
    DateTime SentAt
) : IIntegrationEvent;