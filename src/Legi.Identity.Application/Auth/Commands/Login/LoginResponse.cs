namespace Legi.Identity.Application.Auth.Commands.Login;

public record LoginResponse(
    Guid UserId,
    string Email,
    string Username,
    string Name,
    string? AvatarUrl,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);