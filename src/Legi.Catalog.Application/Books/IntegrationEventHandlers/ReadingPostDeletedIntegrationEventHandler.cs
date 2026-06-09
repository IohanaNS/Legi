using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Library;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Catalog's consumer for <see cref="ReadingPostDeletedIntegrationEvent"/>. When the
/// deleted post was a review, decrements <c>Book.ReviewsCount</c>; otherwise it is a
/// no-op (progress posts are not counted).
///
/// IDEMPOTENCY (§8.1.1): same as the increment counterpart — the inbox is the sole
/// guard, so the handler mutates the tracked Book and MUST NOT call SaveChangesAsync.
///
/// Book not found is a terminal no-op (the book was deleted), not transient — ack
/// without requeue.
/// </summary>
public sealed class ReadingPostDeletedIntegrationEventHandler(
    IBookRepository bookRepository,
    ILogger<ReadingPostDeletedIntegrationEventHandler> logger)
    : INotificationHandler<ReadingPostDeletedIntegrationEvent>
{
    public async Task Handle(
        ReadingPostDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!integrationEvent.IsReview)
            return;

        var book = await bookRepository.GetByIdAsync(integrationEvent.BookId, cancellationToken);
        if (book is null)
        {
            logger.LogWarning(
                "Book {BookId} not found for deleted review {PostId}; terminal no-op (book deleted).",
                integrationEvent.BookId, integrationEvent.PostId);
            return;
        }

        book.DecrementReviewsCount();

        logger.LogDebug(
            "Decremented ReviewsCount on book {BookId} (now {ReviewsCount}) after review {PostId} deleted",
            book.Id, book.ReviewsCount, integrationEvent.PostId);
    }
}
