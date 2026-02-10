using Legi.Identity.Application.Auth.Commands.RefreshToken;
using Legi.Identity.Application.Tests.Factories;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        var command = RefreshTokenCommandFactory.Create(string.Empty);

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
        var command = RefreshTokenCommandFactory.Create();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
