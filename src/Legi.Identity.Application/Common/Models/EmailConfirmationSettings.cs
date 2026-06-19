namespace Legi.Identity.Application.Common.Models;

public class EmailConfirmationSettings
{
    public const string SectionName = "EmailConfirmation";
    public const string ValidationMessage =
        "EmailConfirmation settings must include an absolute http/https FrontendBaseUrl and a positive TokenLifetimeMinutes. " +
        "In production set EmailConfirmation__FrontendBaseUrl to your public site URL.";

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
               Uri.TryCreate(settings.FrontendBaseUrl, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
