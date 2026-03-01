using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public class UserListDeletedDomainEvent(Guid userListId, Guid userId) : IDomainEvent
{
    public Guid UserListId { get; } = userListId;
    public Guid UserId { get; } = userId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}