using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Events;

public sealed class BookUpdatedDomainEvent : IDomainEvent
{
    public Guid BookId { get; }
    public string Isbn { get; }
    public string Title { get; }
    public IReadOnlyList<string> Authors { get; }
    public string? CoverUrl { get; }
    public int? PageCount { get; }
    public DateTime OccurredOn { get; }

    public BookUpdatedDomainEvent(
        Guid bookId,
        string isbn,
        string title,
        IReadOnlyList<string> authors,
        string? coverUrl,
        int? pageCount)
    {
        BookId = bookId;
        Isbn = isbn;
        Title = title;
        Authors = authors;
        CoverUrl = coverUrl;
        PageCount = pageCount;
        OccurredOn = DateTime.UtcNow;
    }
}
