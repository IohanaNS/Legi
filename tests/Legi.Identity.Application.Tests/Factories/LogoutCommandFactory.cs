using Legi.Identity.Application.Auth.Commands.Logout;

namespace Legi.Identity.Application.Tests.Factories;

public static class LogoutCommandFactory
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static LogoutCommand Create(Guid? userId = null, string? refreshToken = null)
    {
        return new LogoutCommand(
            userId ?? DefaultUserId,
            refreshToken ?? "refresh_token_hash"
        );
    }
}
