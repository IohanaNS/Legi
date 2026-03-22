using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Profiles.EventHandlers;

/// <summary>
/// Reacts to a new follow by incrementing counters on both UserProfiles.
/// Lives in Profiles/ (consumer-side convention) because it modifies UserProfile.
/// </summary>
public class FollowCreatedDomainEventHandler(
    IUserProfileRepository userProfileRepository,
    ILogger<FollowCreatedDomainEventHandler> logger)
    : INotificationHandler<FollowCreatedDomainEvent>
{
    public async Task Handle(
        FollowCreatedDomainEvent notification,
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

        followerProfile.IncrementFollowing();
        followedProfile.IncrementFollowers();

        await userProfileRepository.UpdateAsync(followerProfile);
        await userProfileRepository.UpdateAsync(followedProfile);
    }
}