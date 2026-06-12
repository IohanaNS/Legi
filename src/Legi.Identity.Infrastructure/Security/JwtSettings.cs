using System.Text;

namespace Legi.Identity.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int DefaultAccessTokenExpirationMinutes = 60;
    public const int MaxAccessTokenExpirationMinutes = 120;
    public const int MinSecretBytes = 32;
    public const int MaxRefreshTokenExpirationDays = 90;
    public const string AccessTokenExpirationValidationMessage =
        "Jwt:AccessTokenExpirationMinutes must be between 1 and 120 minutes.";
    public const string ValidationMessage =
        "Jwt settings must include a secret of at least 32 bytes, non-empty issuer and audience, " +
        "an access-token lifetime between 1 and 120 minutes, and a refresh-token lifetime between 1 and 90 days.";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = DefaultAccessTokenExpirationMinutes;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public static bool HasValidAccessTokenLifetime(JwtSettings settings)
    {
        return settings.AccessTokenExpirationMinutes is > 0 and <= MaxAccessTokenExpirationMinutes;
    }

    public static bool HasValidSettings(JwtSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.Secret)
            && Encoding.UTF8.GetByteCount(settings.Secret) >= MinSecretBytes
            && !string.IsNullOrWhiteSpace(settings.Issuer)
            && !string.IsNullOrWhiteSpace(settings.Audience)
            && HasValidAccessTokenLifetime(settings)
            && settings.RefreshTokenExpirationDays is > 0 and <= MaxRefreshTokenExpirationDays;
    }

    public void ValidateAccessTokenLifetime()
    {
        if (!HasValidAccessTokenLifetime(this))
            throw new InvalidOperationException(AccessTokenExpirationValidationMessage);
    }

    public void Validate()
    {
        if (!HasValidSettings(this))
            throw new InvalidOperationException(ValidationMessage);
    }
}
