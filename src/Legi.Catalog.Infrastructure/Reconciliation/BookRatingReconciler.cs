using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Reconciliation;

/// <summary>
/// On-demand drift/backfill reconciliation for book ratings (Fase 6 6D.3). Recomputes
/// <c>Book.AverageRating</c>/<c>RatingsCount</c> from the authoritative per-user
/// <c>BookRating</c> projection rows, healing any drift and backfilling ratings that
/// predate Phase 5 (cold-start gap).
///
/// Idempotent and convergent: re-running on already-correct books changes nothing
/// (it only calls <c>RecalculateRating</c> when the value actually differs, so no
/// spurious UpdatedAt bumps or domain events). Manual trigger only — no scheduler
/// until drift is actually observed (the §8.1.4 audit shows the live path is already
/// convergent). Invoke via <c>Legi.Catalog.Api --reconcile-ratings</c>.
/// </summary>
public class BookRatingReconciler(CatalogDbContext context)
{
    /// <summary>Reconciles every book. Returns the number of books whose rating was corrected.</summary>
    public async Task<int> ReconcileAllAsync(CancellationToken cancellationToken = default)
    {
        var books = await context.Books.ToListAsync(cancellationToken);
        var ratingsByBook = (await context.BookRatings.ToListAsync(cancellationToken))
            .GroupBy(r => r.BookId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Rating).ToList());

        var corrected = 0;
        foreach (var book in books)
        {
            var aggregate = ratingsByBook.TryGetValue(book.Id, out var ratings)
                ? BookRatingAggregate.FromHalfStarRatings(ratings)
                : new BookRatingAggregate(0m, 0);

            if (Apply(book, aggregate))
                corrected++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return corrected;
    }

    /// <summary>Reconciles a single book. Returns true if its rating was corrected.</summary>
    public async Task<bool> ReconcileBookAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var book = await context.Books.FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);
        if (book is null)
            return false;

        var ratings = await context.BookRatings
            .Where(r => r.BookId == bookId)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        var changed = Apply(book, BookRatingAggregate.FromHalfStarRatings(ratings));
        if (changed)
            await context.SaveChangesAsync(cancellationToken);
        return changed;
    }

    // Only recompute when the stored value actually differs (compared at the 2dp
    // precision Book persists), so a no-op reconcile stays a true no-op.
    private static bool Apply(Domain.Entities.Book book, BookRatingAggregate aggregate)
    {
        var newAverage = Math.Round(aggregate.Average, 2);
        if (book.AverageRating == newAverage && book.RatingsCount == aggregate.Count)
            return false;

        book.RecalculateRating(aggregate.Average, aggregate.Count);
        return true;
    }
}
