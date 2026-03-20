namespace Legi.Identity.Application.Auth.Commands.Register;

public record RegisterResponse(
    Guid UserId,
    string Email,
    string Username,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);
