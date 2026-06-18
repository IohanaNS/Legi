using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public class ReadingPostDeletedDomainEvent(Guid readingPostId, Guid userId, Guid bookId, Guid workId, bool isReview = false) : IDomainEvent
{
    public Guid ReadingPostId { get; } = readingPostId;
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public Guid WorkId { get; } = workId;
    public bool IsReview { get; } = isReview;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}