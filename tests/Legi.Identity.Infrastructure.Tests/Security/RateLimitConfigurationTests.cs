using System.Text.Json;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class RateLimitConfigurationTests
{
    [Fact]
    public void IdentityAppSettings_ShouldNotTrustClientSuppliedIpHeaders()
    {
        // Arrange
        var appSettings = LoadJson("src", "Legi.Identity.Api", "appsettings.json");
        var ipRateLimiting = appSettings.GetProperty("IpRateLimiting");

        // Assert
        Assert.False(ipRateLimiting.TryGetProperty("RealIpHeader", out _));
        Assert.False(ipRateLimiting.TryGetProperty("ClientIdHeader", out _));
    }

    [Fact]
    public void IdentityAppSettings_ShouldEnableEndpointRateLimiting()
    {
        // Arrange
        var appSettings = LoadJson("src", "Legi.Identity.Api", "appsettings.json");
        var ipRateLimiting = appSettings.GetProperty("IpRateLimiting");

        // Assert
        Assert.True(ipRateLimiting.GetProperty("EnableEndpointRateLimiting").GetBoolean());
    }

    [Fact]
    public void IdentityAppSettings_ShouldConfigureAccountTargetedLoginLockout()
    {
        // Arrange
        var appSettings = LoadJson("src", "Legi.Identity.Api", "appsettings.json");
        var loginLockout = appSettings.GetProperty("LoginLockout");

        // Assert
        Assert.InRange(loginLockout.GetProperty("MaxFailedAttempts").GetInt32(), 1, 10);
        Assert.InRange(loginLockout.GetProperty("FailureWindowMinutes").GetInt32(), 1, 60);
        Assert.InRange(loginLockout.GetProperty("LockoutDurationMinutes").GetInt32(), 1, 60);
    }

    [Fact]
    public void IdentityAppSettings_ShouldConfigureTurnstile()
    {
        // Arrange
        var appSettings = LoadJson("src", "Legi.Identity.Api", "appsettings.json");
        var turnstile = appSettings.GetProperty("Turnstile");

        // Assert
        Assert.False(turnstile.GetProperty("Enabled").GetBoolean());
        Assert.True(turnstile.GetProperty("RequireForRegistration").GetBoolean());
        Assert.InRange(turnstile.GetProperty("LoginFailedAttemptsBeforeRequired").GetInt32(), 0, 5);
        Assert.StartsWith("https://", turnstile.GetProperty("SiteVerifyUrl").GetString());
        Assert.NotEmpty(turnstile.GetProperty("AllowedHostnames").EnumerateArray());
    }

    [Fact]
    public void DockerCompose_ShouldBindIdentityApiToLoopbackOnly()
    {
        // Arrange
        var compose = File.ReadAllText(GetRepoPath("docker-compose.yml"));

        // Assert
        Assert.Contains("\"127.0.0.1:5000:8080\"", compose);
        Assert.DoesNotContain("\"5000:8080\"", compose);
    }

    private static JsonElement LoadJson(params string[] relativePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetRepoPath(relativePath)));
        return document.RootElement.Clone();
    }

    private static string GetRepoPath(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Legi.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. relativePath]);
    }
}
