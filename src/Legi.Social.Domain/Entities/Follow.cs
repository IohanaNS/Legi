using Legi.SharedKernel;
using Legi.Social.Domain.Events;

namespace Legi.Social.Domain.Entities;

public class Follow : BaseEntity
{
    public Guid FollowerId { get; private set; }
    public Guid FollowingId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Follow Create(Guid followerId, Guid followingId)
    {
        if(followerId == followingId)
            throw new DomainException("User cannot follow themselves.");
        
        var follow = new Follow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        };
        follow.AddDomainEvent(new FollowCreatedDomainEvent(follow.FollowerId, follow.FollowingId));
        return follow;
    }
    
    /// <summary>
    /// Marks this follow for removal, raising the domain event
    /// so UserProfile counters can be decremented.
    /// The handler is responsible for calling DeleteAsync on the repository.
    /// </summary>
    public void MarkForRemoval()
    {
        AddDomainEvent(new FollowRemovedDomainEvent(FollowerId, FollowingId));
    }
}