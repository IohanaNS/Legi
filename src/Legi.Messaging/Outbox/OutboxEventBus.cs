using Legi.Contracts;
using Legi.Messaging.Serialization;
using Legi.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Legi.Messaging.Outbox;

/// <summary>
/// <see cref="IEventBus"/> implementation that writes integration events to the
/// outbox table of the current DbContext. The actual publication to the broker is
/// performed asynchronously by <see cref="OutboxDispatcherWorker{TContext}"/>.
/// 
/// Atomicity guarantee: the outbox row is added to the DbContext's change
/// tracker but not committed until the surrounding <c>SaveChangesAsync</c>
/// call commits. If the domain transaction rolls back, the outbox row is
/// discarded — the message never existed.
/// 
/// Footgun: if the caller never calls <c>SaveChangesAsync</c>, the row is
/// silently dropped. In practice this is desired (no save = no message), and
/// the only realistic caller is a domain event handler invoked from the
/// <see cref="DispatchDomainEventsInterceptor"/>, which guarantees a save is
/// in flight.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.3, 2.5, 3.1.
/// </summary>
public class OutboxEventBus<TContext>(TContext context, IntegrationEventSerializer serializer) : IEventBus
    where TContext : DbContext
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        if (@event is not IIntegrationEvent)
            throw new ArgumentException(
                $"Type '{typeof(T).FullName}' is not an IIntegrationEvent. " +
                "Only integration events can be published through IEventBus.",
                nameof(@event));

        var (typeName, payload) = serializer.Serialize(@event);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeName,
            Payload = payload,
            OccurredAt = DateTime.UtcNow,
        };

        context.Add(message);

        // Note: no SaveChangesAsync call here. The row sits in the change
        // tracker until the surrounding transaction commits — that is the
        // entire point of the outbox pattern.
        return Task.CompletedTask;
    }
}