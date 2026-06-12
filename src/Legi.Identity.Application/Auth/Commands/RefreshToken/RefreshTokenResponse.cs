namespace Legi.Identity.Application.Auth.Commands.RefreshToken;

public record RefreshTokenResponse(
    Guid UserId,
    string Email,
    string Username,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime RefreshTokenExpiresAt
);
