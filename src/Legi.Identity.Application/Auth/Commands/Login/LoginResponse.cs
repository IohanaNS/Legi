namespace Legi.Identity.Application.Auth.Commands.Login;

public record LoginResponse(
    Guid UserId,
    string Email,
    string Username,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);
