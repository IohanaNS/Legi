using Legi.Contracts.Catalog;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Social's consumer for <see cref="BookUpdatedIntegrationEvent"/>. Upserts the
/// local <see cref="BookSnapshot"/> so the read model reflects the latest
/// title/authors/cover/page count from Catalog.
///
/// Note: this does NOT back-fill book metadata into existing FeedItem or
/// ContentSnapshot rows — those bake their book fields at creation time and
/// accept staleness (decision 2.6.1). New items created after this point pick
/// up the refreshed snapshot.
///
/// Idempotent: delegates to <c>IBookSnapshotRepository.StageAddOrUpdateAsync</c>.
/// If a <c>BookCreated</c> event has not yet arrived for this book, this
/// handler will create the snapshot.
///
/// MUST NOT call SaveChangesAsync — see MESSAGING-ARCHITECTURE-decisions.md
/// decision 8.1. The IntegrationEventDispatcher commits the inbox row and
/// the snapshot together in a single transaction.
/// </summary>
public sealed class BookUpdatedIntegrationEventHandler(
    IBookSnapshotRepository bookSnapshotRepository,
    ILogger<BookUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<BookUpdatedIntegrationEvent>
{
    public async Task Handle(
        BookUpdatedIntegrationEvent integrationEvent,
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
            "Staged BookSnapshot update for book {BookId} from BookUpdatedIntegrationEvent",
            integrationEvent.BookId);
    }
}
