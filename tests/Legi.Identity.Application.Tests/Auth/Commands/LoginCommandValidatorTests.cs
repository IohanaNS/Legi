using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Tests.Factories;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenEmailOrUsernameIsEmpty()
    {
        // Arrange
        var command = LoginCommandFactory.Create(emailOrUsername: string.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Email or username is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsEmpty()
    {
        // Arrange
        var command = LoginCommandFactory.Create(password: string.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password is required");
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = LoginCommandFactory.Create();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
