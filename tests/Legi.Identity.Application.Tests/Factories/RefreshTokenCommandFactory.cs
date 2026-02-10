using Legi.Identity.Application.Auth.Commands.RefreshToken;

namespace Legi.Identity.Application.Tests.Factories;

public static class RefreshTokenCommandFactory
{
    public static RefreshTokenCommand Create(string? refreshToken = null)
    {
        return new RefreshTokenCommand(refreshToken ?? "refresh_token_hash");
    }
}
