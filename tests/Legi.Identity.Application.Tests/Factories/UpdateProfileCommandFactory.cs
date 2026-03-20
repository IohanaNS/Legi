using Legi.Identity.Application.Users.Commands.UpdateProfile;

namespace Legi.Identity.Application.Tests.Factories;

public static class UpdateProfileCommandFactory
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static UpdateProfileCommand Create(
        Guid? userId = null,
        bool isPublicProfile = true)
    {
        return new UpdateProfileCommand(
            userId ?? DefaultUserId,
            isPublicProfile
        );
    }
}
