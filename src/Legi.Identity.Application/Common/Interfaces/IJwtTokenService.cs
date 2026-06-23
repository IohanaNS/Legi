using Legi.Identity.Domain.Entities;

namespace Legi.Identity.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetRefreshTokenExpiresAt();

    /// <summary>
    /// Issues a short-lived token proving the first factor passed, scoped to a distinct
    /// audience so it can never be used as an access token against the resource APIs.
    /// </summary>
    string GenerateMfaChallengeToken(User user);

    /// <summary>Validates an MFA challenge token and returns the user id, or null if invalid/expired.</summary>
    Guid? ValidateMfaChallengeToken(string token);

    /// <summary>
    /// Issues a short-lived token proving the user recently verified account deletion.
    /// </summary>
    string GenerateAccountDeletionChallengeToken(User user);

    /// <summary>Validates an account deletion challenge token and returns the user id, or null if invalid/expired.</summary>
    Guid? ValidateAccountDeletionChallengeToken(string token);
}
