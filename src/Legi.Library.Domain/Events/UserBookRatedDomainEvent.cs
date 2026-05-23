using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class UserBookRatedDomainEvent(Guid userId, Guid bookId, Rating? oldRating, Rating newRating)
    : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public Rating? OldRating { get; } = oldRating;
    public Rating NewRating { get; } = newRating;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
