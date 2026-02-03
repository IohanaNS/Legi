namespace Legi.Identity.Application.Auth.Commands.Register;

public record RegisterResponse(
    Guid UserId,
    string Email,
    string Username,
    string Name,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);