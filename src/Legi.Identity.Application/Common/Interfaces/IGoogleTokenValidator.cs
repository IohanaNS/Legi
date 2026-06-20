using Legi.Identity.Application.Common.Models;

namespace Legi.Identity.Application.Common.Interfaces;

public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates a Google ID token (signature, issuer, audience, expiry) and returns
    /// the contained user information, or <c>null</c> if the token is invalid.
    /// </summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
