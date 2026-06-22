namespace Legi.Identity.Application.Common.Models;

internal static class FrontendBaseUrlValidator
{
    public static bool IsValid(string frontendBaseUrl)
    {
        if (!Uri.TryCreate(frontendBaseUrl, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps ||
               (uri.Scheme == Uri.UriSchemeHttp && IsLocalhost(uri));
    }

    private static bool IsLocalhost(Uri uri)
    {
        return uri.IsLoopback ||
               string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }
}
