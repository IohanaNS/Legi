using Legi.Social.Domain.Entities;

namespace Legi.Social.Application.Tests.Factories;

public static class FollowFactory
{
    public static Follow Create(Guid? followerId = null, Guid? followingId = null)
    {
        var follower = followerId ?? Guid.NewGuid();
        var followed = followingId ?? Guid.NewGuid();

        return Follow.Create(follower, followed);
    }
}
