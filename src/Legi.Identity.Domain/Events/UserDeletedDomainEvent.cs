using Legi.SharedKernel;

namespace Legi.Identity.Domain.Events;

public sealed class UserDeletedDomainEvent(Guid userId) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}