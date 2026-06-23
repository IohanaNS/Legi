using Legi.Identity.Domain.Enums;

namespace Legi.Identity.Application.Auth.Commands.Login;

public record LoginResponse(
    Guid UserId,
    string Email,
    string Username,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime RefreshTokenExpiresAt,
    // When true, the password was correct but a second factor is required: the token
    // fields are empty and the client must complete login with MfaToken + a code.
    bool MfaRequired = false,
    string? MfaToken = null,
    // Which second factor to prompt for when MfaRequired (so the client knows whether to
    // ask for an authenticator code or to request/await an emailed code).
    MfaMethod MfaMethod = MfaMethod.None
);
