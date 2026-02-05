using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Events;

public sealed class BookTagsUpdatedDomainEvent(Guid bookId, IReadOnlyList<string> tags) : IDomainEvent
{
    public Guid BookId { get; } = bookId;
    public IReadOnlyList<string> Tags { get; } = tags;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}