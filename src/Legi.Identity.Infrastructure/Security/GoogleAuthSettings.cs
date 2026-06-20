namespace Legi.Identity.Infrastructure.Security;

public class GoogleAuthSettings
{
    public const string SectionName = "GoogleAuth";
    public const string ValidationMessage =
        "GoogleAuth:ClientId must be configured to enable Google sign-in.";

    public string ClientId { get; set; } = string.Empty;

    public static bool HasValidSettings(GoogleAuthSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.ClientId);
    }
}
