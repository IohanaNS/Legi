using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Library;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Catalog's consumer for <see cref="UserBookRatingRemovedIntegrationEvent"/>.
/// Removes the user's row from Catalog's local <c>BookRating</c> projection
/// (delete by book+user, no-op if absent), then recomputes
/// <c>Book.AverageRating</c>/<c>RatingsCount</c> from the remaining ratings.
/// <c>RemovedRating</c> from the event is not read — the delete is keyed by
/// book+user.
///
/// IDEMPOTENCY (Phase 5, Option B): delete-by-key and recompute-from-rows are
/// convergent — the inbox is defense-in-depth, not the sole guard. Stages in the
/// change tracker; the dispatcher owns the single SaveChanges (BookRating delete +
/// Book update + inbox row, atomic — decision 8.1). MUST NOT call SaveChangesAsync
/// or ExecuteUpdate/Delete.
///
/// Book not found → throw → nack-with-requeue (transient, same policy as the rated
/// handler). When the last rating is removed the recompute yields (0, 0).
/// </summary>
public sealed class UserBookRatingRemovedIntegrationEventHandler(
    IBookRepository bookRepository,
    IBookRatingRepository bookRatingRepository,
    ILogger<UserBookRatingRemovedIntegrationEventHandler> logger)
    : INotificationHandler<UserBookRatingRemovedIntegrationEvent>
{
    public async Task Handle(
        UserBookRatingRemovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(integrationEvent.BookId, cancellationToken);
        if (book is null)
        {
            logger.LogWarning(
                "Book {BookId} not found for UserBookRatingRemoved; throwing to redeliver.",
                integrationEvent.BookId);
            throw new TransientMessagingException(
                $"Book {integrationEvent.BookId} not found; cannot recompute rating.");
        }

        var aggregate = await bookRatingRepository.StageRatingRemovalAsync(
            integrationEvent.BookId,
            integrationEvent.UserId,
            cancellationToken);

        book.RecalculateRating(aggregate.Average, aggregate.Count);

        logger.LogDebug(
            "Recomputed rating for book {BookId} after rating removal by user {UserId}: avg {Average}, count {Count}",
            integrationEvent.BookId, integrationEvent.UserId, aggregate.Average, aggregate.Count);
    }
}
