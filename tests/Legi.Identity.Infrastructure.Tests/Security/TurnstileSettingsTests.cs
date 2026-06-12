using Legi.Identity.Application.Common.Models;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class TurnstileSettingsTests
{
    [Fact]
    public void HasValidSettings_ShouldAllowDisabledSettingsWithoutSecret()
    {
        // Arrange
        var settings = new TurnstileSettings
        {
            Enabled = false,
            SecretKey = string.Empty
        };

        // Act
        var result = TurnstileSettings.HasValidSettings(settings);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValidSettings_ShouldRejectEnabledSettingsWithoutSecret()
    {
        // Arrange
        var settings = new TurnstileSettings
        {
            Enabled = true,
            SecretKey = string.Empty
        };

        // Act
        var result = TurnstileSettings.HasValidSettings(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasValidSettings_ShouldAllowEnabledSettingsWithHttpsSiteVerifyUrl()
    {
        // Arrange
        var settings = new TurnstileSettings
        {
            Enabled = true,
            SecretKey = "1x0000000000000000000000000000000AA",
            SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify"
        };

        // Act
        var result = TurnstileSettings.HasValidSettings(settings);

        // Assert
        Assert.True(result);
    }
}
