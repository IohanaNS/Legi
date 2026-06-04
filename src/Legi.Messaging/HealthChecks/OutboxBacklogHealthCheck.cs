using Legi.Messaging.Outbox;
using Legi.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Legi.Messaging.HealthChecks;

/// <summary>
/// Reports the size of the unprocessed outbox backlog (Fase 6 6C). A growing
/// backlog is the early warning that the dispatcher is stuck or the broker is
/// down. <b>Degraded</b> (not Unhealthy) past the threshold: messages are safe in
/// the outbox and will drain once publishing resumes — the service itself is fine.
/// </summary>
public sealed class OutboxBacklogHealthCheck<TContext>(
    TContext context, IOptions<MessagingHostingOptions> options) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context_, CancellationToken cancellationToken = default)
    {
        var threshold = options.Value.OutboxBacklogThreshold;

        int pending;
        try
        {
            pending = await context.Set<OutboxMessage>()
                .CountAsync(m => m.ProcessedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot read the outbox backlog.", ex);
        }

        var data = new Dictionary<string, object> { ["pending"] = pending, ["threshold"] = threshold };

        return pending > threshold
            ? HealthCheckResult.Degraded(
                $"Outbox backlog {pending} exceeds threshold {threshold}.", data: data)
            : HealthCheckResult.Healthy($"Outbox backlog {pending} within threshold {threshold}.", data);
    }
}
