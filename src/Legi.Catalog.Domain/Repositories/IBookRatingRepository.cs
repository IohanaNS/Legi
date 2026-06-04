namespace Legi.Catalog.Domain.Repositories;

/// <summary>
/// Recomputed rating aggregate for a single book, in the Catalog's 0-5 display
/// scale. Produced from the half-star (1-10) per-user ratings Catalog mirrors
/// locally.
/// </summary>
public readonly record struct BookRatingAggregate(decimal Average, int Count)
{
    /// <summary>
    /// Computes the aggregate from a book's half-star ratings (each 1-10).
    /// Empty input → (0, 0) — the book has no ratings. Otherwise the average is
    /// the mean half-star value converted to the 0-5 scale (÷2); rounding to 2dp
    /// is applied downstream by <c>Book.RecalculateRating</c>.
    ///
    /// Pure/deterministic so it is unit-testable without a database.
    /// </summary>
    public static BookRatingAggregate FromHalfStarRatings(IReadOnlyCollection<int> halfStarRatings)
    {
        var count = halfStarRatings.Count;
        if (count == 0)
            return new BookRatingAggregate(0m, 0);

        var average = halfStarRatings.Sum() / (decimal)count / 2m;
        return new BookRatingAggregate(average, count);
    }
}

/// <summary>
/// Write access to Catalog's local projection of per-user book ratings (sourced
/// from Library via integration events, Phase 5).
///
/// Both methods STAGE their change in the change tracker and return the aggregate
/// recomputed over the book's ratings INCLUDING the staged change — they do NOT
/// call SaveChangesAsync. The <c>IntegrationEventDispatcher</c> owns the single
/// commit, so the rating-row change, the <c>Book</c> update, and the inbox row
/// commit atomically (decision 8.1). The recompute is computed in-memory from the
/// tracked rows precisely because the staged change is not yet visible to a SQL
/// AVG/COUNT query.
/// </summary>
public interface IBookRatingRepository
{
    /// <summary>
    /// Upserts the user's rating for the book (insert, or update on re-rate) and
    /// returns the recomputed aggregate. Idempotent by natural key (BookId, UserId).
    /// </summary>
    Task<BookRatingAggregate> StageRatingAsync(
        Guid bookId, Guid userId, int halfStarRating, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the user's rating for the book (no-op if absent) and returns the
    /// recomputed aggregate.
    /// </summary>
    Task<BookRatingAggregate> StageRatingRemovalAsync(
        Guid bookId, Guid userId, CancellationToken cancellationToken = default);
}
