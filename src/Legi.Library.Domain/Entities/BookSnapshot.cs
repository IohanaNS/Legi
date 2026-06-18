namespace Legi.Library.Domain.Entities;

/// <summary>
/// Read model with denormalized book data from the Catalog service.
/// Updated via integration events (BookCreated, BookUpdated).
/// Not an aggregate — no domain logic, no domain events.
/// </summary>
public class BookSnapshot
{
    public Guid BookId { get; private set; }

    /// <summary>
    /// The Catalog work this book (edition) belongs to. Nullable only for snapshots
    /// created before the Work/Edition split — those are populated when Catalog
    /// re-projects (BookUpdated). New snapshots always carry it.
    /// </summary>
    public Guid? WorkId { get; private set; }

    public string Title { get; private set; } = null!;
    public string AuthorDisplay { get; private set; } = null!;
    public string? CoverUrl { get; private set; }
    public int? PageCount { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static BookSnapshot Create(
        Guid bookId,
        string title,
        string authorDisplay,
        string? coverUrl,
        int? pageCount,
        Guid? workId)
    {
        return new BookSnapshot
        {
            BookId = bookId,
            WorkId = workId,
            Title = title,
            AuthorDisplay = authorDisplay,
            CoverUrl = coverUrl,
            PageCount = pageCount,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string authorDisplay,
        string? coverUrl,
        int? pageCount,
        Guid? workId)
    {
        Title = title;
        AuthorDisplay = authorDisplay;
        CoverUrl = coverUrl;
        PageCount = pageCount;
        // Only overwrite a known work id — never regress a populated one to null
        // (e.g. an old in-flight message that predates WorkId).
        if (workId.HasValue)
        {
            WorkId = workId;
        }
        UpdatedAt = DateTime.UtcNow;
    }
}