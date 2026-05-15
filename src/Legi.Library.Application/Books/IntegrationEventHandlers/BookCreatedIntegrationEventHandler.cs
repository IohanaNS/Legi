using Legi.Contracts.Catalog;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Library's consumer for <see cref="BookCreatedIntegrationEvent"/>. Creates a
/// local <see cref="BookSnapshot"/> read model for the new book so Library can
/// resolve book display data (title, cover, authors) without calling Catalog.
///
/// Idempotent: delegates to <c>IBookSnapshotRepository.StageAddOrUpdateAsync</c>,
/// which updates an existing row or adds a new one. The inbox guarantees this
/// handler runs at most once per message, but pre-existing snapshots
/// (e.g. leftover from the inline-create workaround) are still possible during
/// the transition.
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
