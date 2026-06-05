using Legi.Social.Domain.Entities;

namespace Legi.Social.Domain.Tests.Factories;

public static class UserProfileFactory
{
    public static UserProfile Create(Guid? userId = null, string username = "reader")
    {
        return UserProfile.Create(userId ?? SocialTestIds.UserId, username);
    }
}
