using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class BookAddedToLibraryDomainEvent(Guid userId, Guid bookId, bool wishList = false) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public bool WishList { get; } = wishList;
}