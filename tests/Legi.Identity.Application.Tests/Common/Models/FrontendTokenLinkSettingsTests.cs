using Legi.Identity.Application.Common.Models;

namespace Legi.Identity.Application.Tests.Common.Models;

public class FrontendTokenLinkSettingsTests
{
    [Theory]
    [InlineData("https://bukihub.example")]
    [InlineData("https://bukihub.example/app")]
    [InlineData("http://localhost:3000")]
    [InlineData("http://127.0.0.1:3000")]
    [InlineData("http://[::1]:3000")]
    public void PasswordResetSettings_ShouldAcceptHttpsOrLocalHttpFrontendBaseUrl(string frontendBaseUrl)
    {
        var settings = new PasswordResetSettings
        {
            FrontendBaseUrl = frontendBaseUrl,
            TokenLifetimeMinutes = 60
        };

        Assert.True(PasswordResetSettings.HasValidSettings(settings));
    }

    [Theory]
    [InlineData("http://bukihub.example")]
    [InlineData("ftp://bukihub.example")]
    [InlineData("/reset-password")]
    [InlineData("")]
    public void PasswordResetSettings_ShouldRejectNonHttpsNonLocalFrontendBaseUrl(string frontendBaseUrl)
    {
        var settings = new PasswordResetSettings
        {
            FrontendBaseUrl = frontendBaseUrl,
            TokenLifetimeMinutes = 60
        };

        Assert.False(PasswordResetSettings.HasValidSettings(settings));
    }

    [Fact]
    public void PasswordResetSettings_ShouldRejectNonPositiveTokenLifetime()
    {
        var settings = new PasswordResetSettings
        {
            FrontendBaseUrl = "https://bukihub.example",
            TokenLifetimeMinutes = 0
        };

        Assert.False(PasswordResetSettings.HasValidSettings(settings));
    }

    [Theory]
    [InlineData("https://bukihub.example")]
    [InlineData("https://bukihub.example/app")]
    [InlineData("http://localhost:3000")]
    [InlineData("http://127.0.0.1:3000")]
    [InlineData("http://[::1]:3000")]
    public void EmailConfirmationSettings_ShouldAcceptHttpsOrLocalHttpFrontendBaseUrl(string frontendBaseUrl)
    {
        var settings = new EmailConfirmationSettings
        {
            FrontendBaseUrl = frontendBaseUrl,
            TokenLifetimeMinutes = 1440
        };

        Assert.True(EmailConfirmationSettings.HasValidSettings(settings));
    }

    [Theory]
    [InlineData("http://bukihub.example")]
    [InlineData("ftp://bukihub.example")]
    [InlineData("/confirm-email")]
    [InlineData("")]
    public void EmailConfirmationSettings_ShouldRejectNonHttpsNonLocalFrontendBaseUrl(string frontendBaseUrl)
    {
        var settings = new EmailConfirmationSettings
        {
            FrontendBaseUrl = frontendBaseUrl,
            TokenLifetimeMinutes = 1440
        };

        Assert.False(EmailConfirmationSettings.HasValidSettings(settings));
    }

    [Fact]
    public void EmailConfirmationSettings_ShouldRejectNonPositiveTokenLifetime()
    {
        var settings = new EmailConfirmationSettings
        {
            FrontendBaseUrl = "https://bukihub.example",
            TokenLifetimeMinutes = 0
        };

        Assert.False(EmailConfirmationSettings.HasValidSettings(settings));
    }
}
