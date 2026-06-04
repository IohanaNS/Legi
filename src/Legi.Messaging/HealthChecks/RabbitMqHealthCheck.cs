using Legi.Messaging.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Legi.Messaging.HealthChecks;

/// <summary>
/// Reports whether the shared RabbitMQ connection is open (Fase 6 6C).
///
/// Deliberately NON-blocking: it inspects the current connection snapshot and
/// never attempts to (re)establish one — attempting a connect would hang the
/// /health endpoint for seconds when the broker is down. A previously-open
/// connection that is now closed → Unhealthy (fast). "Not yet established" (a
/// producer-only service idle since startup) is reported Healthy; sustained
/// broker-down for such a service surfaces via the outbox-backlog check, which is
/// the durable backstop.
/// </summary>
public sealed class RabbitMqHealthCheck(RabbitMqConnectionFactory connectionFactory) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connection = connectionFactory.CurrentConnection;

        var result = connection switch
        {
            null => HealthCheckResult.Healthy("RabbitMQ connection not yet established."),
            { IsOpen: true } => HealthCheckResult.Healthy("RabbitMQ connection is open."),
            _ => HealthCheckResult.Unhealthy("RabbitMQ connection is not open.")
        };

        return Task.FromResult(result);
    }
}
