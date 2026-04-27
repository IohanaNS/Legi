namespace Legi.SharedKernel;

/// <summary>
/// Outbound port for publishing integration events across bounded contexts.
/// 
/// The Application layer depends only on this interface; the concrete
/// implementation (<c>OutboxEventBus&lt;TContext&gt;</c>) lives in
/// <c>Legi.Messaging</c> and writes the message to the <c>outbox_messages</c>
/// table of the current DbContext, guaranteeing transactional atomicity with
/// the domain changes.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.3 and 3.1.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class;
}