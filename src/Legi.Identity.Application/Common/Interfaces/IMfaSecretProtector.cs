namespace Legi.Identity.Application.Common.Interfaces;

/// <summary>
/// Reversibly encrypts TOTP shared secrets for storage. Unlike passwords, TOTP secrets
/// must be recoverable (needed to verify codes), so they are encrypted rather than hashed.
/// </summary>
public interface IMfaSecretProtector
{
    string Protect(string plaintext);

    /// <summary>Decrypts a value produced by <see cref="Protect"/>; throws if it was tampered with.</summary>
    string Unprotect(string protectedValue);
}
