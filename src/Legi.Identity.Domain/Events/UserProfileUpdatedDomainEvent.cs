using Legi.SharedKernel;

namespace Legi.Identity.Domain.Events;

public sealed class UserProfileUpdatedDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Name { get; }
    public string? Bio { get; }
    public string? AvatarUrl { get; }
    public DateTime OccurredOn { get; }

    public UserProfileUpdatedDomainEvent(Guid userId, string name, string? bio, string? avatarUrl)
    {
        UserId = userId;
        Name = name;
        Bio = bio;
        AvatarUrl = avatarUrl;
        OccurredOn = DateTime.UtcNow;
    }
}