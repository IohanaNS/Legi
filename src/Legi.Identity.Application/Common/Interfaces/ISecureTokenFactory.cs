namespace Legi.Identity.Application.Common.Interfaces;

public interface ISecureTokenFactory
{
    /// <summary>
    /// Generates a new URL-safe token together with its hash for storage.
    /// </summary>
    (string Token, string Hash) Create();

    /// <summary>
    /// Hashes an inbound token so it can be matched against stored hashes.
    /// </summary>
    string Hash(string token);
}
