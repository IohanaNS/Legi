using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Events;

public sealed class BookCreatedDomainEvent : IDomainEvent
{
    public Guid BookId { get; }
    public string Isbn { get; }
    public string Title { get; }
    public IReadOnlyList<string> Authors { get; }
    public string? CoverUrl { get; }
    public int? PageCount { get; }
    public Guid CreatedByUserId { get; }
    public Guid WorkId { get; }
    public DateTime OccurredOn { get; }

    public BookCreatedDomainEvent(
        Guid bookId,
        string isbn,
        string title,
        IReadOnlyList<string> authors,
        string? coverUrl,
        int? pageCount,
        Guid createdByUserId,
        Guid workId)
    {
        BookId = bookId;
        Isbn = isbn;
        Title = title;
        Authors = authors;
        CoverUrl = coverUrl;
        PageCount = pageCount;
        CreatedByUserId = createdByUserId;
        WorkId = workId;
        OccurredOn = DateTime.UtcNow;
    }
}
