using Legi.Identity.Application.Auth.Commands.Register;
using Legi.Identity.Application.Tests.Factories;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenEmailFormatIsInvalid()
    {
        // Arrange
        var command = RegisterCommandFactory.Create(email: "invalid-email");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invalid email format");
    }

    [Fact]
    public void Validate_ShouldFail_WhenUsernameFormatIsInvalid()
    {
        // Arrange
        var command = RegisterCommandFactory.Create(username: "Invalid-Username");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage ==
                 "Username must start with a letter and contain only lowercase letters, numbers and underscore"
        );
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsWeak()
    {
        // Arrange
        var command = RegisterCommandFactory.Create(password: "weakpass");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password must contain at least one uppercase letter");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password must contain at least one number");
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = RegisterCommandFactory.Create();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
