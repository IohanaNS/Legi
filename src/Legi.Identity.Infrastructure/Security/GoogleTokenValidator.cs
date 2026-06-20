using Google.Apis.Auth;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Infrastructure.Security;

public class GoogleTokenValidator(
    GoogleAuthSettings settings,
    ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        try
        {
            var settingsForValidation = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [settings.ClientId]
            };

            // Validates signature, issuer (accounts.google.com), audience and expiry.
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settingsForValidation);

            if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
                return null;

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Rejected invalid Google ID token");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Transient failures (e.g. fetching Google's signing certs) — treat as a
            // failed validation rather than surfacing a 500 to the caller.
            logger.LogWarning(ex, "Google ID token validation failed unexpectedly");
            return null;
        }
    }
}
