namespace Legi.Identity.Application.Common.Models;

public class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";
    public const string ValidationMessage =
        "PasswordReset settings must include an absolute http/https FrontendBaseUrl and a positive TokenLifetimeMinutes. " +
        "In production set PasswordReset__FrontendBaseUrl to your public site URL.";

    /// <summary>
    /// Public base URL of the web frontend, used to build the reset link sent by email.
    /// Has no default on purpose: a localhost fallback would silently produce broken links in
    /// production. Dev supplies it via appsettings.Development.json; other environments must set it.
    /// </summary>
    public string FrontendBaseUrl { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 60;

    public static bool HasValidSettings(PasswordResetSettings settings)
    {
        return settings.TokenLifetimeMinutes > 0 &&
               Uri.TryCreate(settings.FrontendBaseUrl, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
