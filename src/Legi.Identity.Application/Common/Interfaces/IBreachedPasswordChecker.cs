namespace Legi.Identity.Application.Common.Interfaces;

/// <summary>
/// Checks whether a password is known to have appeared in a public data breach.
/// Implementations MUST be privacy-preserving (never transmit the raw password) and
/// SHOULD fail open — returning <c>false</c> when the check cannot be completed — so a
/// third-party outage never blocks registration or password reset.
/// </summary>
public interface IBreachedPasswordChecker
{
    Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default);
}
