namespace Legi.Identity.Api.Security;

internal static class RefreshTokenCookie
{
    public const string Name = "legi.refreshToken";
    private const string CookiePath = "/api/v1/identity/auth";

    public static void Append(
        HttpResponse response,
        string refreshToken,
        DateTime expiresAt,
        IHostEnvironment environment)
    {
        response.Cookies.Append(
            Name,
            refreshToken,
            CreateOptions(expiresAt, environment));
    }

    public static void Delete(HttpResponse response, IHostEnvironment environment)
    {
        response.Cookies.Delete(
            Name,
            CreateOptions(DateTime.UtcNow.AddDays(-1), environment));
    }

    private static CookieOptions CreateOptions(DateTime expiresAt, IHostEnvironment environment)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = CookiePath,
            IsEssential = true
        };
    }
}
