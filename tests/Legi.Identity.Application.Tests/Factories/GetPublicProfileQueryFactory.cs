using Legi.Identity.Application.Users.Queries.GetPublicProfile;

namespace Legi.Identity.Application.Tests.Factories;

public static class GetPublicProfileQueryFactory
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DefaultCurrentUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static GetPublicProfileQuery Create(
        Guid? userId = null,
        Guid? currentUserId = null)
    {
        return new GetPublicProfileQuery(
            userId ?? DefaultUserId,
            currentUserId
        );
    }

    public static GetPublicProfileQuery CreateAuthenticated(Guid? userId = null)
    {
        return Create(userId, DefaultCurrentUserId);
    }
}
