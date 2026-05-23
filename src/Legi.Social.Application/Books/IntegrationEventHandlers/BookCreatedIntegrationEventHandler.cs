using Legi.Contracts.Catalog;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Social's consumer for <see cref="BookCreatedIntegrationEvent"/>. Creates a
/// local <see cref="BookSnapshot"/> read model so the Library → Social event
/// handlers (4C) can resolve book display data (title, author, cover) at
/// write time when baking FeedItem/ContentSnapshot rows (decision 2.6.1).
///
/// Idempotent: delegates to <c>IBookSnapshotRepository.StageAddOrUpdateAsync</c>,
/// which updates an existing row or adds a new one.
///
/// MUST NOT call SaveChangesAsync — see MESSAGING-ARCHITECTURE-decisions.md
/// decision 8.1. The IntegrationEventDispatcher commits the inbox row and
/// the snapshot together in a single transaction.
/// </summary>
public sealed class BookCreatedIntegrationEventHandler(
    IBookSnapshotRepository bookSnapshotRepository,
    ILogger<BookCreatedIntegrationEventHandler> logger)
    : INotificationHandler<BookCreatedIntegrationEvent>
{
    public async Task Handle(
        BookCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var authorDisplay = string.Join(", ", integrationEvent.Authors);

        var snapshot = BookSnapshot.Create(
            bookId: integrationEvent.BookId,
            title: integrationEvent.Title,
            authorDisplay: authorDisplay,
            coverUrl: integrationEvent.CoverUrl,
            pageCount: integrationEvent.PageCount);

        await bookSnapshotRepository.StageAddOrUpdateAsync(snapshot, cancellationToken);

        logger.LogInformation(
            "Staged BookSnapshot for book {BookId} from BookCreatedIntegrationEvent",
            integrationEvent.BookId);
    }
}
