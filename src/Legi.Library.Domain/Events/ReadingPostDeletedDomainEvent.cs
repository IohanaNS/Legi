using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public class ReadingPostDeletedDomainEvent(Guid readingPostId, Guid userId, Guid bookId) : IDomainEvent
{
    public Guid ReadingPostId { get; } = readingPostId;
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}