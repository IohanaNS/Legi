namespace Legi.Identity.Application.Auth.Commands.RefreshToken;

public record RefreshTokenResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);