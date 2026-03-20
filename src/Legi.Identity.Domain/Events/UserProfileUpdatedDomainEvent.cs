using Legi.SharedKernel;

namespace Legi.Identity.Domain.Events;

public sealed class UserProfileUpdatedDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public bool IsPublicProfile { get; }
    public DateTime OccurredOn { get; }

    public UserProfileUpdatedDomainEvent(Guid userId, bool isPublicProfile)
    {
        UserId = userId;
        IsPublicProfile = isPublicProfile;
        OccurredOn = DateTime.UtcNow;
    }
}
