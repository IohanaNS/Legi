using Legi.Social.Domain.Entities;

namespace Legi.Social.Application.Tests.Factories;

public static class UserProfileFactory
{
    public static UserProfile Create(
        Guid? userId = null,
        string username = "reader")
    {
        return UserProfile.Create(userId ?? Guid.NewGuid(), username);
    }

    public static UserProfile CreateDetailed(
        Guid? userId = null,
        string username = "reader",
        string? bio = "Collects science fiction paperbacks.",
        string? avatarUrl = "https://cdn.example.com/avatar.png",
        string? bannerUrl = "https://cdn.example.com/banner.png",
        int followersCount = 0,
        int followingCount = 0)
    {
        var profile = Create(userId, username);

        profile.UpdateBio(bio);
        profile.UpdateAvatar(avatarUrl);
        profile.UpdateBanner(bannerUrl);

        for (var i = 0; i < followersCount; i++)
            profile.IncrementFollowers();

        for (var i = 0; i < followingCount; i++)
            profile.IncrementFollowing();

        return profile;
    }
}
