using Legi.SharedKernel;

namespace Legi.Identity.Domain.Events;

public sealed class UserUsernameChangedDomainEvent(Guid userId, string newUsername) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public string NewUsername { get; } = newUsername;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
