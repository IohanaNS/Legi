using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Library;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Catalog's consumer for <see cref="ReviewCreatedIntegrationEvent"/>. Increments
/// <c>Book.ReviewsCount</c>.
///
/// IDEMPOTENCY (MESSAGING-ARCHITECTURE-decisions.md §8.1.1): an increment is not
/// convergent, so the inbox is the ONLY guard against double-counting. The handler
/// MUST NOT call SaveChangesAsync; it mutates the tracked Book and lets the
/// <c>IntegrationEventDispatcher</c> commit the counter change and the inbox row in
/// a single transaction. Mirrors Library's ContentLiked counter handler.
///
/// Book not found = the review arrived before <c>BookCreated</c> was consumed:
/// throw → nack-with-requeue (transient), retry until the book exists.
/// </summary>
public sealed class ReviewCreatedIntegrationEventHandler(
    IBookRepository bookRepository,
    IWorkRepository workRepository,
    ILogger<ReviewCreatedIntegrationEventHandler> logger)
    : INotificationHandler<ReviewCreatedIntegrationEvent>
{
    public async Task Handle(
        ReviewCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(integrationEvent.BookId, cancellationToken);
        if (book is null)
        {
            logger.LogWarning(
                "Book {BookId} not found for ReviewCreated; throwing to redeliver " +
                "(review likely arrived before BookCreated was consumed).",
                integrationEvent.BookId);
            throw new TransientMessagingException(
                $"Book {integrationEvent.BookId} not found; cannot increment reviews count.");
        }

        book.IncrementReviewsCount();

        // The reviews count shown on the book page is the work's (across editions).
        var work = await workRepository.GetByIdAsync(book.WorkId, cancellationToken);
        work?.IncrementReviewsCount();

        logger.LogDebug(
            "Incremented ReviewsCount on book {BookId} (now {ReviewsCount})",
            book.Id, book.ReviewsCount);
    }
}
