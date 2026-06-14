namespace Legi.Identity.Application.Common.Interfaces;

public interface IPasswordResetTokenFactory
{
    /// <summary>
    /// Generates a new URL-safe reset token together with its hash for storage.
    /// </summary>
    (string Token, string Hash) Create();

    /// <summary>
    /// Hashes an inbound reset token so it can be matched against stored hashes.
    /// </summary>
    string Hash(string token);
}
