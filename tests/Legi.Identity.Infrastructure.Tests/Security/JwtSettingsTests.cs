using Legi.Identity.Infrastructure.Security;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class JwtSettingsTests
{
    private const string StrongSecret = "abcdefghijklmnopqrstuvwxyz123456";

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

    [Fact]
    public void Validate_ShouldAllowStrongSettings()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var exception = Record.Exception(settings.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_ShouldRejectMissingSecret()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Secret = string.Empty;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        // Assert
        Assert.Equal(JwtSettings.ValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldRejectWeakSecret()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Secret = "too-short";

        // Act
        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        // Assert
        Assert.Equal(JwtSettings.ValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldRejectMissingIssuer()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Issuer = string.Empty;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        // Assert
        Assert.Equal(JwtSettings.ValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldRejectInvalidRefreshTokenLifetime()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.RefreshTokenExpirationDays = 365;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        // Assert
        Assert.Equal(JwtSettings.ValidationMessage, exception.Message);
    }

    private static JwtSettings CreateValidSettings()
    {
        return new JwtSettings
        {
            Secret = StrongSecret,
            Issuer = "Legi.Identity.Tests",
            Audience = "Legi.Tests",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
    }
}
