using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Library;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Catalog's consumer for <see cref="UserBookRatedIntegrationEvent"/>. Mirrors the
/// user's rating into Catalog's local <c>BookRating</c> projection (upsert by
/// book+user), then recomputes <c>Book.AverageRating</c>/<c>RatingsCount</c> from
/// the complete set of the book's ratings.
///
/// IDEMPOTENCY (Phase 5, Option B — the INVERSE of the §8.1.1 counter rule):
/// upsert-by-(BookId,UserId) and recompute-from-rows are convergent/self-healing,
/// so the inbox is defense-in-depth here, NOT the sole guard. The handler stages
/// its changes in the change tracker and lets the <c>IntegrationEventDispatcher</c>
/// perform the single SaveChanges that commits the BookRating row, the Book update,
/// and the inbox row atomically (decision 8.1). It MUST NOT call SaveChangesAsync,
/// and must NOT use ExecuteUpdate/Delete (which would commit before the inbox row).
///
/// Book not found = the rating arrived before <c>BookCreated</c> (Catalog → Catalog
/// ordering window, same as 4C's missing BookSnapshot): throw → nack-with-requeue
/// (transient), retry until the book exists. Never a terminal drop.
///
/// This is the second consumer on the existing <c>UserBookRated</c> fanout exchange;
/// Social's BookRated feed handler is the other, independent queue.
/// </summary>
public sealed class UserBookRatedIntegrationEventHandler(
    IBookRepository bookRepository,
    IBookRatingRepository bookRatingRepository,
    ILogger<UserBookRatedIntegrationEventHandler> logger)
    : INotificationHandler<UserBookRatedIntegrationEvent>
{
    public async Task Handle(
        UserBookRatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(integrationEvent.BookId, cancellationToken);
        if (book is null)
        {
            logger.LogWarning(
                "Book {BookId} not found for UserBookRated; throwing to redeliver " +
                "(rating likely arrived before BookCreated was consumed).",
                integrationEvent.BookId);
            throw new InvalidOperationException(
                $"Book {integrationEvent.BookId} not found; cannot recompute rating.");
        }

        var aggregate = await bookRatingRepository.StageRatingAsync(
            integrationEvent.BookId,
            integrationEvent.UserId,
            integrationEvent.Rating,
            cancellationToken);

        book.RecalculateRating(aggregate.Average, aggregate.Count);

        logger.LogDebug(
            "Recomputed rating for book {BookId} after rating by user {UserId}: avg {Average}, count {Count}",
            integrationEvent.BookId, integrationEvent.UserId, aggregate.Average, aggregate.Count);
    }
}
