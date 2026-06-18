using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Entities;

/// <summary>
/// The abstract book — the thing readers think of as "a book" independent of any
/// particular printing. Groups one or more <see cref="Book"/>s (editions) that
/// share a <see cref="ValueObjects.WorkKey"/>. See Docs/CATALOG-FEATURE-editions.md.
///
/// Intentionally lean in this increment: identity + a representative title/cover
/// for edition-agnostic display. Aggregate rating/reviews and authors move here in
/// a later increment; until then they remain on <see cref="Book"/>.
/// </summary>
public class Work : BaseAuditableEntity
{
    public WorkKey WorkKey { get; private set; } = null!;
    public string Title { get; private set; } = null!;

    /// <summary>
    /// A representative cover for showing the work where no specific edition is
    /// chosen (search, lists, feed). Denormalized from one of its editions.
    /// </summary>
    public string? DefaultCoverUrl { get; private set; }

    /// <summary>
    /// Aggregate rating across the work's editions (0-5, 2dp), and the number of
    /// ratings behind it. Maintained when an edition's rating changes — readers
    /// rate the work, so this is the rating shown on the book page. See the
    /// attachment map in Docs/CATALOG-FEATURE-editions.md.
    /// </summary>
    public decimal AverageRating { get; private set; }
    public int RatingsCount { get; private set; }

    /// <summary>
    /// Number of reviews across the work's editions. Maintained when a review is
    /// created/deleted in Library. The reviews count shown on the book page.
    /// </summary>
    public int ReviewsCount { get; private set; }

    private Work() { }

    public static Work Create(WorkKey workKey, string title, string? defaultCoverUrl = null)
    {
        ArgumentNullException.ThrowIfNull(workKey);

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Work title is required");

        return new Work
        {
            Id = Guid.NewGuid(),
            WorkKey = workKey,
            Title = title.Trim(),
            DefaultCoverUrl = string.IsNullOrWhiteSpace(defaultCoverUrl) ? null : defaultCoverUrl.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets the representative cover only if the work doesn't have one yet — the
    /// first edition to contribute a cover wins, later editions don't clobber it.
    /// </summary>
    public void EnsureDefaultCover(string? coverUrl)
    {
        if (!string.IsNullOrWhiteSpace(DefaultCoverUrl) || string.IsNullOrWhiteSpace(coverUrl))
            return;

        DefaultCoverUrl = coverUrl.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the work's aggregate rating, recomputed across its editions when one of
    /// them is rated. Mirrors <see cref="Book.RecalculateRating"/>: average in 0-5,
    /// rounded to 2dp; count non-negative.
    /// </summary>
    public void RecalculateRating(decimal newAverage, int totalRatings)
    {
        if (newAverage < 0 || newAverage > 5)
            throw new DomainException("Average rating must be between 0 and 5");

        if (totalRatings < 0)
            throw new DomainException("Total ratings cannot be negative");

        AverageRating = Math.Round(newAverage, 2);
        RatingsCount = totalRatings;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Increments the reviews count when a review is created in Library.</summary>
    public void IncrementReviewsCount()
    {
        ReviewsCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Decrements the reviews count when a review is deleted. Never below zero.</summary>
    public void DecrementReviewsCount()
    {
        if (ReviewsCount > 0)
            ReviewsCount--;
        UpdatedAt = DateTime.UtcNow;
    }
}
