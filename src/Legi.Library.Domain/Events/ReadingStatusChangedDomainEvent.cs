using Legi.Library.Domain.Enums;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class ReadingStatusChangedDomainEvent(
    Guid userId,
    Guid bookId,
    ReadingStatus oldStatus,
    ReadingStatus newStatus) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public ReadingStatus OldStatus { get; } = oldStatus;
    public ReadingStatus NewStatus { get; } = newStatus;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}