using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Profiles.EventHandlers;

/// <summary>
/// Reacts to an unfollow by decrementing counters on both UserProfiles.
/// Lives in Profiles/ (consumer-side convention) because it modifies UserProfile.
/// </summary>
public class FollowRemovedDomainEventHandler(
    IUserProfileRepository userProfileRepository,
    ILogger<FollowRemovedDomainEventHandler> logger)
    : INotificationHandler<FollowRemovedDomainEvent>
{
    public async Task Handle(
        FollowRemovedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var followerProfile = await userProfileRepository.GetByUserIdAsync(notification.FollowerId);
        var followedProfile = await userProfileRepository.GetByUserIdAsync(notification.FollowingId);

        if (followerProfile is null || followedProfile is null)
        {
            logger.LogWarning(
                "Could not update follow counters — profile not found. FollowerId: {FollowerId}, FollowingId: {FollowingId}",
                notification.FollowerId, notification.FollowingId);
            return;
        }

        followerProfile.DecrementFollowing();
        followedProfile.DecrementFollowers();

        await userProfileRepository.UpdateAsync(followerProfile);
        await userProfileRepository.UpdateAsync(followedProfile);
    }
}