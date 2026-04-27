using Legi.SharedKernel.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Legi.SharedKernel;

/// <summary>
/// Dispatches domain events BEFORE <c>base.SaveChangesAsync()</c>, within the
/// EF Core transaction. Guarantees that:
/// <list type="bullet">
///   <item>Domain event handlers run in the same transaction that persists the changes</item>
///   <item>Integration events published via <see cref="IEventBus"/> are written to
///         the outbox in the SAME transaction that persists the domain changes</item>
///   <item>Either everything commits or nothing does — no inconsistency window</item>
/// </list>
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.3 and 2.5.
/// </summary>
public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Iteration cap for the dispatch loop. Safeguard against accidental cycles
    /// in handlers that mutate tracked entities and raise new events. 10 is far
    /// above realistic depth (typical flows are 1-2 levels) and acts as a bug
    /// detector instead of an infinite silent hang.
    /// </summary>
    private const int MaxDispatchIterations = 10;

    private readonly IMediator _mediator;

    public DispatchDomainEventsInterceptor(IMediator mediator)
        => _mediator = mediator;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext ctx, CancellationToken ct)
    {
        for (var iteration = 0; iteration < MaxDispatchIterations; iteration++)
        {
            var entitiesWithEvents = ctx.ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            if (entitiesWithEvents.Count == 0)
                return;

            // Snapshot BEFORE clearing — if a handler raises new events (by
            // mutating another entity), they survive for the next iteration.
            // Clearing after dispatch would discard those new events.
            var events = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            foreach (var entity in entitiesWithEvents)
                entity.ClearDomainEvents();

            foreach (var @event in events)
                await _mediator.Publish(@event, ct);
        }

        throw new InvalidOperationException(
            $"Domain event dispatch exceeded {MaxDispatchIterations} iterations. " +
            "Likely cycle: a handler is raising events that are also processed " +
            "by handlers raising new events. Investigate the active handlers in " +
            "the current flow.");
    }
}