using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class UserBookRatedDomainEvent(Guid userBookId, Guid bookId, Rating? oldRating, Rating newRating)
    : IDomainEvent
{
    public Guid UserBookId { get; } = userBookId;
    public Guid BookId { get; } = bookId;
    public Rating? OldRating { get; } = oldRating;
    public Rating NewRating { get; } = newRating;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}