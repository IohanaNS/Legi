using System.Net;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Infrastructure.Security;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class TurnstileVerificationServiceTests
{
    [Fact]
    public async Task VerifyAsync_ShouldPassWhenResponseMatchesExpectedActionAndHostname()
    {
        // Arrange
        var service = CreateService("""
            {
              "success": true,
              "action": "login",
              "hostname": "bukihub.com"
            }
            """);

        // Act
        var result = await service.VerifyAsync("token", "127.0.0.1", "login");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyAsync_ShouldFailWhenActionDoesNotMatch()
    {
        // Arrange
        var service = CreateService("""
            {
              "success": true,
              "action": "register",
              "hostname": "bukihub.com"
            }
            """);

        // Act
        var result = await service.VerifyAsync("token", "127.0.0.1", "login");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyAsync_ShouldFailWhenHostnameIsNotAllowed()
    {
        // Arrange
        var service = CreateService("""
            {
              "success": true,
              "action": "login",
              "hostname": "evil.example"
            }
            """);

        // Act
        var result = await service.VerifyAsync("token", "127.0.0.1", "login");

        // Assert
        Assert.False(result);
    }

    private static TurnstileVerificationService CreateService(string responseJson)
    {
        var settings = new TurnstileSettings
        {
            Enabled = true,
            SecretKey = "secret",
            SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify",
            AllowedHostnames = ["bukihub.com"]
        };

        return new TurnstileVerificationService(
            settings,
            new HttpClient(new StubHttpMessageHandler(responseJson)));
    }

    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            });
        }
    }
}
