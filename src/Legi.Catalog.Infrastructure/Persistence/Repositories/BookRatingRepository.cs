using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Per-user book rating projection (Phase 5, Option B). Stages upserts/removals in
/// the change tracker and recomputes the aggregate in-memory from the tracked rows
/// — never calls SaveChangesAsync (the dispatcher commits, decision 8.1) and never
/// uses ExecuteUpdate/Delete (which would commit before the inbox row). The
/// in-memory recompute is deliberate: a staged-but-unsaved row is invisible to a
/// SQL AVG/COUNT, so we adjust the loaded tracked list and aggregate from it.
/// </summary>
public class BookRatingRepository(CatalogDbContext context) : IBookRatingRepository
{
    public async Task<BookRatingAggregate> StageRatingAsync(
        Guid bookId, Guid userId, int halfStarRating, CancellationToken cancellationToken = default)
    {
        var rows = await LoadTrackedAsync(bookId, cancellationToken);

        var existing = rows.FirstOrDefault(r => r.UserId == userId);
        if (existing is null)
        {
            var row = new BookRatingEntity
            {
                BookId = bookId,
                UserId = userId,
                Rating = halfStarRating,
                UpdatedAt = DateTime.UtcNow
            };
            await context.BookRatings.AddAsync(row, cancellationToken);
            rows.Add(row);
        }
        else
        {
            existing.Rating = halfStarRating;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        return BookRatingAggregate.FromHalfStarRatings(rows.Select(r => r.Rating).ToList());
    }

    public async Task<BookRatingAggregate> StageRatingRemovalAsync(
        Guid bookId, Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await LoadTrackedAsync(bookId, cancellationToken);

        var existing = rows.FirstOrDefault(r => r.UserId == userId);
        if (existing is not null)
        {
            context.BookRatings.Remove(existing);
            rows.Remove(existing);
        }

        return BookRatingAggregate.FromHalfStarRatings(rows.Select(r => r.Rating).ToList());
    }

    // Tracked load (no AsNoTracking) so staged Add/Remove/updates are reflected and
    // committed by the dispatcher's single SaveChanges.
    private async Task<List<BookRatingEntity>> LoadTrackedAsync(
        Guid bookId, CancellationToken cancellationToken)
    {
        return await context.BookRatings
            .Where(r => r.BookId == bookId)
            .ToListAsync(cancellationToken);
    }
}
