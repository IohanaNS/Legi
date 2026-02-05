using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Events;

public sealed class BookCreatedDomainEvent(
    Guid bookId,
    string isbn,
    string title,
    string author,
    Guid createdByUserId)
    : IDomainEvent
{
    public Guid BookId { get; } = bookId;
    public string Isbn { get; } = isbn;
    public string Title { get; } = title;
    public string Author { get; } = author;
    public Guid CreatedByUserId { get; } = createdByUserId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}