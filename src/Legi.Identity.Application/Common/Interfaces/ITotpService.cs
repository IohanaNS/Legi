namespace Legi.Identity.Application.Common.Interfaces;

/// <summary>
/// Time-based one-time password (TOTP, RFC 6238) operations for authenticator-app MFA.
/// </summary>
public interface ITotpService
{
    /// <summary>Generates a new random Base32-encoded shared secret.</summary>
    string GenerateSecret();

    /// <summary>
    /// Builds the <c>otpauth://</c> URI an authenticator app encodes as a QR code.
    /// </summary>
    string BuildOtpAuthUri(string base32Secret, string accountName, string issuer);

    /// <summary>
    /// Verifies a 6-digit code against the secret, allowing ±1 time step (±30s) for clock
    /// drift. Comparison is constant-time.
    /// </summary>
    bool VerifyCode(string base32Secret, string code, DateTimeOffset? now = null);
}
