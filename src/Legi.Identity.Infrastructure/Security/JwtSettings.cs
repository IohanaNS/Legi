using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Legi.Identity.Infrastructure.Security;

/// <summary>
/// JWT configuration. Access tokens are signed with RSA (RS256): only the
/// Identity service holds the <see cref="PrivateKey"/> and can mint tokens; every
/// service validates with the <see cref="PublicKey"/> and can never forge one.
/// Keys are PEM, accepted either raw or base64-encoded (single-line, env-friendly).
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int DefaultAccessTokenExpirationMinutes = 15;
    public const int MaxAccessTokenExpirationMinutes = 120;
    public const int MaxRefreshTokenExpirationDays = 90;
    public const string AccessTokenExpirationValidationMessage =
        "Jwt:AccessTokenExpirationMinutes must be between 1 and 120 minutes.";
    public const string ValidationMessage =
        "Jwt settings must include a valid RSA PublicKey (PEM or base64-encoded PEM), a non-empty " +
        "issuer and audience, an access-token lifetime between 1 and 120 minutes, and a refresh-token " +
        "lifetime between 1 and 90 days.";
    public const string SigningValidationMessage =
        "Jwt signing settings (Identity service) require a valid RSA PrivateKey (PEM or base64-encoded " +
        "PEM) in addition to the validation settings.";

    /// <summary>RSA public key (PEM or base64-encoded PEM). Required by every service.</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>RSA private key (PEM or base64-encoded PEM). Required only by Identity (signing).</summary>
    public string PrivateKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = DefaultAccessTokenExpirationMinutes;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public static bool HasValidAccessTokenLifetime(JwtSettings settings)
    {
        return settings.AccessTokenExpirationMinutes is > 0 and <= MaxAccessTokenExpirationMinutes;
    }

    /// <summary>Validation settings — enough to <i>verify</i> tokens (public key only).</summary>
    public static bool HasValidSettings(JwtSettings settings)
    {
        return IsValidRsaKey(settings.PublicKey, requirePrivate: false)
            && !string.IsNullOrWhiteSpace(settings.Issuer)
            && !string.IsNullOrWhiteSpace(settings.Audience)
            && HasValidAccessTokenLifetime(settings)
            && settings.RefreshTokenExpirationDays is > 0 and <= MaxRefreshTokenExpirationDays;
    }

    /// <summary>Signing settings — required by Identity to <i>mint</i> tokens (private key too).</summary>
    public static bool HasValidSigningSettings(JwtSettings settings)
    {
        return HasValidSettings(settings) && IsValidRsaKey(settings.PrivateKey, requirePrivate: true);
    }

    public void ValidateAccessTokenLifetime()
    {
        if (!HasValidAccessTokenLifetime(this))
            throw new InvalidOperationException(AccessTokenExpirationValidationMessage);
    }

    /// <summary>Called by Identity at startup; requires both keys (it signs and validates).</summary>
    public void Validate()
    {
        if (!HasValidSigningSettings(this))
            throw new InvalidOperationException(SigningValidationMessage);
    }

    /// <summary>Public key for token validation. The returned key lives for the app lifetime.</summary>
    public RsaSecurityKey CreatePublicSigningKey()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(DecodePem(PublicKey));
        return new RsaSecurityKey(rsa);
    }

    /// <summary>RS256 signing credentials (Identity only). Lives for the app lifetime.</summary>
    public SigningCredentials CreateSigningCredentials()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(DecodePem(PrivateKey));
        return new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
    }

    private static bool IsValidRsaKey(string value, bool requirePrivate)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(DecodePem(value));
            if (requirePrivate)
                // Throws if the PEM carried only public parameters.
                _ = rsa.ExportParameters(includePrivateParameters: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string DecodePem(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Contains("-----BEGIN", StringComparison.Ordinal))
            return trimmed;

        // Otherwise treat it as base64-encoded PEM (the single-line, env-var-friendly form).
        return Encoding.UTF8.GetString(Convert.FromBase64String(trimmed));
    }
}
