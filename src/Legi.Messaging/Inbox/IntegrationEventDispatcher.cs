using Legi.Contracts;
using Legi.SharedKernel.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Legi.Messaging.Inbox;

/// <summary>
/// Dispatches a deserialized integration event to its handlers via
/// <see cref="IMediator"/>, with inbox-based deduplication.
/// 
/// Owns the consumer-side transaction. The flow is:
/// <list type="number">
///   <item>Open a fresh DbContext scope</item>
///   <item>Check the inbox for the MessageId — if present, skip silently</item>
///   <item>Add an <see cref="InboxMessage"/> row to the change tracker</item>
///   <item>Invoke <c>IMediator.Publish</c>; handlers mutate the same DbContext</item>
///   <item>Call <c>SaveChangesAsync</c> once — inbox row + handler changes
///         commit atomically, or both roll back together</item>
/// </list>
/// 
/// Handlers MUST NOT call <c>SaveChangesAsync</c> themselves — see decision 8.1.
/// 
/// Generic over <typeparamref name="TContext"/> so each service registers a
/// dispatcher pointed at its own DbContext.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 3.3 and 8.1.
/// </summary>
public class IntegrationEventDispatcher<TContext>
    where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationEventDispatcher<TContext>> _logger;

    public IntegrationEventDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrationEventDispatcher<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches a single message. Returns true if the message was
    /// processed (or was a duplicate that should be considered handled).
    /// Throws on genuine processing failure — caller decides what to do
    /// with the broker delivery (typically nack-with-requeue).
    /// </summary>
    public async Task DispatchAsync(
        Guid messageId,
        string typeName,
        IIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Dedup: if we have already processed this MessageId, the handler
        // has already done its work in a previous delivery. Silently skip.
        var alreadyProcessed = await ctx.Set<InboxMessage>()
            .AsNoTracking()
            .AnyAsync(m => m.Id == messageId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogDebug(
                "Integration event {MessageId} of type {Type} is a duplicate; skipping",
                messageId, typeName);
            return;
        }

        // Stage the inbox row BEFORE the handler runs. If the handler
        // succeeds, SaveChangesAsync commits both the row and the handler's
        // changes atomically. If the handler throws, we never call
        // SaveChangesAsync — both are discarded, the broker redelivers,
        // we try again.
        ctx.Add(new InboxMessage
        {
            Id = messageId,
            Type = typeName,
            ProcessedAt = DateTime.UtcNow,
        });

        await mediator.Publish(@event, cancellationToken);

        // One save, one commit. Inbox + handler changes go together.
        await ctx.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Integration event {MessageId} of type {Type} processed",
            messageId, typeName);
    }
}