using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public class UserBookRatingRemovedDomainEvent(Guid userId, Guid bookId, Rating oldRating) : IDomainEvent
{
    public Guid UserId { get; } =  userId;
    public Guid BookId { get; } =  bookId;
    public Rating OldRating { get; } = oldRating;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}