using Legi.Identity.Domain.Common;

namespace Legi.Identity.Domain.Events;

public sealed class UserDeletedDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public DateTime OccurredOn { get; }

    public UserDeletedDomainEvent(Guid userId)
    {
        UserId = userId;
        OccurredOn = DateTime.UtcNow;
    }
}