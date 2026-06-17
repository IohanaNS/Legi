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
}
