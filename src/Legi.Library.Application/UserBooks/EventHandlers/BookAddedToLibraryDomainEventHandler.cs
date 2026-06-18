using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.UserBooks.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="BookAddedToLibraryDomainEvent"/> into
/// the cross-context <see cref="BookAddedToLibraryIntegrationEvent"/> and
/// publishes it via <see cref="IEventBus"/>.
///
/// Runs synchronously within the producer's SaveChangesAsync transaction
/// (via DispatchDomainEventsInterceptor). The integration event is staged
/// into the outbox and committed together with the new UserBook.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class BookAddedToLibraryDomainEventHandler
    : INotificationHandler<BookAddedToLibraryDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<BookAddedToLibraryDomainEventHandler> _logger;

    public BookAddedToLibraryDomainEventHandler(
        IEventBus eventBus,
        ILogger<BookAddedToLibraryDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        BookAddedToLibraryDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new BookAddedToLibraryIntegrationEvent(
            UserBookId: domainEvent.UserBookId,
            UserId: domainEvent.UserId,
            BookId: domainEvent.BookId,
            Wishlist: domainEvent.WishList,
            AddedAt: domainEvent.OccurredOn,
            WorkId: domainEvent.WorkId);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated BookAddedToLibraryDomainEvent for user {UserId}, book {BookId} to integration event",
            domainEvent.UserId,
            domainEvent.BookId);
    }
}
