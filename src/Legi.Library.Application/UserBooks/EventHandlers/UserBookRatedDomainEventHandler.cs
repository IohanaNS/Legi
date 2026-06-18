using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.UserBooks.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="UserBookRatedDomainEvent"/> into the
/// cross-context <see cref="UserBookRatedIntegrationEvent"/> and publishes it
/// via <see cref="IEventBus"/>.
///
/// The domain event carries <see cref="Legi.Library.Domain.ValueObjects.Rating"/>
/// value objects; the integration contract uses primitive ints (half-stars,
/// 1-10). Mapped via <c>.Value</c> at the boundary.
///
/// Future consumer (Phase 5) is Catalog (rating recompute). Current consumer
/// (Phase 4) is Social (BookRated feed item).
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class UserBookRatedDomainEventHandler
    : INotificationHandler<UserBookRatedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserBookRatedDomainEventHandler> _logger;

    public UserBookRatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<UserBookRatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        UserBookRatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserBookRatedIntegrationEvent(
            BookId: domainEvent.BookId,
            UserId: domainEvent.UserId,
            Rating: domainEvent.NewRating.Value,
            PreviousRating: domainEvent.OldRating?.Value,
            WorkId: domainEvent.WorkId,
            IsPartOfReview: domainEvent.IsPartOfReview);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated UserBookRatedDomainEvent for user {UserId}, book {BookId} to integration event",
            domainEvent.UserId,
            domainEvent.BookId);
    }
}
