using Legi.SharedKernel;

namespace Legi.Social.Domain.Events;

public sealed class FollowCreatedDomainEvent (Guid followerId, Guid followingId) : IDomainEvent
{
    public Guid FollowerId { get; } = followerId;
    public Guid FollowingId { get; } = followingId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}