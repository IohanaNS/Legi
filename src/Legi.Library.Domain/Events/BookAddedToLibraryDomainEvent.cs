using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class BookAddedToLibraryDomainEvent(
    Guid userBookId,
    Guid userId,
    Guid bookId,
    Guid workId,
    bool wishList = false) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid UserBookId { get; } = userBookId;
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public Guid WorkId { get; } = workId;
    public bool WishList { get; } = wishList;
}
