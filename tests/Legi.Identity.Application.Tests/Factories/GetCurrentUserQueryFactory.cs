using Legi.Identity.Application.Users.Queries.GetCurrentUser;

namespace Legi.Identity.Application.Tests.Factories;

public static class GetCurrentUserQueryFactory
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static GetCurrentUserQuery Create(Guid? userId = null)
    {
        return new GetCurrentUserQuery(userId ?? DefaultUserId);
    }
}
