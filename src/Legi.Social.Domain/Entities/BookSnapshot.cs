namespace Legi.Social.Domain.Entities;

/// <summary>
/// Read model with denormalized book data from the Catalog service.
/// Updated via integration events (BookCreated, BookUpdated).
/// Not an aggregate — no domain logic, no domain events.
///
/// Used as a write-time lookup source by Library → Social event handlers when
/// creating FeedItem/ContentSnapshot rows (decision 2.6.1). Not joined at
/// read time — FeedItem/ContentSnapshot bake title/author/cover at creation.
/// </summary>
public class BookSnapshot
{
    public Guid BookId { get; private set; }
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
        int? pageCount)
    {
        return new BookSnapshot
        {
            BookId = bookId,
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
        int? pageCount)
    {
        Title = title;
        AuthorDisplay = authorDisplay;
        CoverUrl = coverUrl;
        PageCount = pageCount;
        UpdatedAt = DateTime.UtcNow;
    }
}
