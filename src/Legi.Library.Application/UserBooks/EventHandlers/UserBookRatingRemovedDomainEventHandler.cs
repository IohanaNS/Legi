using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.UserBooks.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="UserBookRatingRemovedDomainEvent"/> into
/// the cross-context <see cref="UserBookRatingRemovedIntegrationEvent"/> and
/// publishes it via <see cref="IEventBus"/>.
///
/// The domain event carries a <see cref="Legi.Library.Domain.ValueObjects.Rating"/>
/// value object; the integration contract uses the primitive half-star int (1-10),
/// mapped via <c>.Value</c> at the boundary.
///
/// Sole consumer is Catalog (Phase 5): it removes the user's rating row and
/// recomputes the book average. This is the publisher half deferred from Phase 4B
/// (no consumer existed then; building it now would have produced outbox rows for
/// zero bound queues).
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class UserBookRatingRemovedDomainEventHandler
    : INotificationHandler<UserBookRatingRemovedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserBookRatingRemovedDomainEventHandler> _logger;

    public UserBookRatingRemovedDomainEventHandler(
        IEventBus eventBus,
        ILogger<UserBookRatingRemovedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        UserBookRatingRemovedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserBookRatingRemovedIntegrationEvent(
            BookId: domainEvent.BookId,
            UserId: domainEvent.UserId,
            RemovedRating: domainEvent.OldRating.Value);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated UserBookRatingRemovedDomainEvent for user {UserId}, book {BookId} to integration event",
            domainEvent.UserId,
            domainEvent.BookId);
    }
}
