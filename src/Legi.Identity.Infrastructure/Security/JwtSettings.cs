namespace Legi.Identity.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int DefaultAccessTokenExpirationMinutes = 60;
    public const int MaxAccessTokenExpirationMinutes = 120;
    public const string AccessTokenExpirationValidationMessage =
        "Jwt:AccessTokenExpirationMinutes must be between 1 and 120 minutes.";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = DefaultAccessTokenExpirationMinutes;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public static bool HasValidAccessTokenLifetime(JwtSettings settings)
    {
        return settings.AccessTokenExpirationMinutes is > 0 and <= MaxAccessTokenExpirationMinutes;
    }

    public void ValidateAccessTokenLifetime()
    {
        if (!HasValidAccessTokenLifetime(this))
            throw new InvalidOperationException(AccessTokenExpirationValidationMessage);
    }
}
