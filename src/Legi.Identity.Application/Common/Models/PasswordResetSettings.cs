namespace Legi.Identity.Application.Common.Models;

public class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";
    public const string ValidationMessage =
        "PasswordReset settings must include an absolute HTTPS FrontendBaseUrl, or HTTP localhost for local development, " +
        "and a positive TokenLifetimeMinutes.";

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
               FrontendBaseUrlValidator.IsValid(settings.FrontendBaseUrl);
    }
}
