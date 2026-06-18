using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Recomputes a work's aggregate rating after one of its editions' rating changed.
/// Composes per-edition aggregates (a weighted average across editions) rather than
/// re-aggregating the raw per-user rows — so it stays clear of the idempotency-
/// sensitive in-memory recompute in <c>BookRatingRepository</c>. The just-rated
/// edition's new aggregate is read from its tracked <see cref="Book"/> (already
/// updated in this transaction); its siblings' committed aggregates come from a SQL
/// sum.
///
/// Staged (not saved) — the IntegrationEventDispatcher's single SaveChanges commits
/// the work update alongside the rating row and the book update (decision 8.1).
///
/// Note: editions are not de-duplicated per user (a reader who rated two editions of
/// the same work counts twice). Exact for single-edition works (today's data);
/// refined when work-level rating lands in Library.
/// </summary>
internal static class WorkRatingRecalculator
{
    public static async Task RecalculateAsync(
        Book book,
        IBookRepository bookRepository,
        IWorkRepository workRepository,
        CancellationToken cancellationToken)
    {
        var work = await workRepository.GetByIdAsync(book.WorkId, cancellationToken);
        if (work is null)
        {
            // The work should always exist for a persisted book; skip defensively
            // rather than fail the rating recompute.
            return;
        }

        var siblings = await bookRepository.GetEditionRatingTotalsForWorkAsync(
            book.WorkId, book.Id, cancellationToken);

        var totalCount = siblings.RatingsCount + book.RatingsCount;
        var totalWeighted = siblings.WeightedRatingSum + book.AverageRating * book.RatingsCount;
        var average = totalCount == 0 ? 0m : totalWeighted / totalCount;

        work.RecalculateRating(average, totalCount);
    }
}
