using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class UserBookRatedDomainEvent(
    Guid userId, Guid bookId, Guid workId, Rating? oldRating, Rating newRating, bool isPartOfReview = false)
    : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public Guid WorkId { get; } = workId;
    public Rating? OldRating { get; } = oldRating;
    public Rating NewRating { get; } = newRating;

    /// <summary>
    /// True when the rating was set while writing a review, so Social can suppress
    /// the standalone BookRated feed item (the review emits its own activity).
    /// </summary>
    public bool IsPartOfReview { get; } = isPartOfReview;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
