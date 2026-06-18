using Legi.Contracts.Catalog;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Library's consumer for <see cref="BookUpdatedIntegrationEvent"/>. Upserts
/// the local <see cref="BookSnapshot"/> so the read model reflects the latest
/// title/authors/cover/page count from Catalog.
///
/// Idempotent: delegates to <c>IBookSnapshotRepository.StageAddOrUpdateAsync</c>,
/// which updates an existing row or adds a new one. If a <c>BookCreated</c>
/// has not yet arrived for this book, this handler will create the snapshot.
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
            pageCount: integrationEvent.PageCount,
            // Guid.Empty only from old in-flight messages predating the split.
            workId: integrationEvent.WorkId == Guid.Empty ? null : integrationEvent.WorkId);

        await bookSnapshotRepository.StageAddOrUpdateAsync(snapshot, cancellationToken);

        logger.LogInformation(
            "Staged BookSnapshot update for book {BookId} from BookUpdatedIntegrationEvent",
            integrationEvent.BookId);
    }
}
