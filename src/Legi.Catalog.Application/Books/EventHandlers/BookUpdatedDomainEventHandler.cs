using Legi.Catalog.Domain.Events;
using Legi.Contracts.Catalog;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="BookUpdatedDomainEvent"/> into the
/// cross-context <see cref="BookUpdatedIntegrationEvent"/> and publishes it
/// via <see cref="IEventBus"/>.
///
/// Runs synchronously within the producer's SaveChangesAsync transaction
/// (via DispatchDomainEventsInterceptor). The integration event is staged
/// into the outbox table and committed together with the book changes.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class BookUpdatedDomainEventHandler
    : INotificationHandler<BookUpdatedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<BookUpdatedDomainEventHandler> _logger;

    public BookUpdatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<BookUpdatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        BookUpdatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new BookUpdatedIntegrationEvent(
            BookId: domainEvent.BookId,
            Isbn: domainEvent.Isbn,
            Title: domainEvent.Title,
            Authors: domainEvent.Authors.ToList(),
            CoverUrl: domainEvent.CoverUrl,
            PageCount: domainEvent.PageCount);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated BookUpdatedDomainEvent for book {BookId} to integration event",
            domainEvent.BookId);
    }
}
