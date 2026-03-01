using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class ReadingPostCreatedDomainEvent(Guid readingPostId, Guid userBookId, Guid userId, Guid bookId)
    : IDomainEvent
{
    public Guid ReadingPostId { get; } = readingPostId;
    public Guid UserBookId { get; } = userBookId;
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}