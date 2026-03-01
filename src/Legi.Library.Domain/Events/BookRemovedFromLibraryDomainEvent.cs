using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class BookRemovedFromLibraryDomainEvent(Guid userBookId, Guid userId, Guid bookId) : IDomainEvent
{
    public Guid BookId { get; } = bookId;
    public Guid UserId { get; } = userId;
    public Guid UserBookId { get; } = userBookId;
    public DateTime OccurredOn { get; } =  DateTime.UtcNow;
}