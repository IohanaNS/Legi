using Legi.Identity.Infrastructure.Security;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class JwtSettingsTests
{
    [Fact]
    public void ValidateAccessTokenLifetime_ShouldAllowDefaultLifetime()
    {
        // Arrange
        var settings = new JwtSettings();

        // Act
        var exception = Record.Exception(settings.ValidateAccessTokenLifetime);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAccessTokenLifetime_ShouldRejectMultiDayLifetime()
    {
        // Arrange
        var settings = new JwtSettings
        {
            AccessTokenExpirationMinutes = 50_000
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            settings.ValidateAccessTokenLifetime);

        // Assert
        Assert.Equal(JwtSettings.AccessTokenExpirationValidationMessage, exception.Message);
    }
}
