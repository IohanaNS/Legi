using Legi.Identity.Application.Auth.Commands.Logout;
using Legi.Identity.Application.Tests.Factories;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = LogoutCommandFactory.Create(userId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "UserId is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        var command = LogoutCommandFactory.Create(refreshToken: string.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Refresh token is required");
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = LogoutCommandFactory.Create();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
