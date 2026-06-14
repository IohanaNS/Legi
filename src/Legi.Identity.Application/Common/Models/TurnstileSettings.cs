namespace Legi.Identity.Application.Common.Models;

public class TurnstileSettings
{
    public const string SectionName = "Turnstile";
    public const string ValidationMessage =
        "Turnstile settings must include positive thresholds and, when enabled, a secret, HTTPS siteverify URL, and allowed hostnames.";

    public bool Enabled { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string SiteVerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    public string[] AllowedHostnames { get; set; } = [];
    public bool RequireForRegistration { get; set; } = true;
    public bool RequireForPasswordReset { get; set; } = true;
    public int LoginFailedAttemptsBeforeRequired { get; set; } = 2;
    public int VerificationTimeoutSeconds { get; set; } = 5;

    public static bool HasValidSettings(TurnstileSettings settings)
    {
        if (settings.LoginFailedAttemptsBeforeRequired < 0 ||
            settings.VerificationTimeoutSeconds <= 0)
        {
            return false;
        }

        if (!settings.Enabled)
            return true;

        return !string.IsNullOrWhiteSpace(settings.SecretKey) &&
               Uri.TryCreate(settings.SiteVerifyUrl, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps &&
               settings.AllowedHostnames.Length > 0 &&
               settings.AllowedHostnames.All(IsValidHostname);
    }

    private static bool IsValidHostname(string hostname)
    {
        var trimmed = hostname.Trim();
        return trimmed.Length > 0 &&
               !trimmed.Contains("://", StringComparison.Ordinal) &&
               Uri.CheckHostName(trimmed) != UriHostNameType.Unknown;
    }
}
