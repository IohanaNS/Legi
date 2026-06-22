namespace Legi.Identity.Application.Common.Models;

public class EmailConfirmationSettings
{
    public const string SectionName = "EmailConfirmation";
    public const string ValidationMessage =
        "EmailConfirmation settings must include an absolute HTTPS FrontendBaseUrl, or HTTP localhost for local development, " +
        "and a positive TokenLifetimeMinutes.";

    /// <summary>
    /// Public base URL of the web frontend, used to build the confirmation link sent by email.
    /// Has no default on purpose: a localhost fallback would silently produce broken links in
    /// production. Dev supplies it via appsettings.Development.json; other environments must set it.
    /// </summary>
    public string FrontendBaseUrl { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 1440;

    public static bool HasValidSettings(EmailConfirmationSettings settings)
    {
        return settings.TokenLifetimeMinutes > 0 &&
               FrontendBaseUrlValidator.IsValid(settings.FrontendBaseUrl);
    }
}
