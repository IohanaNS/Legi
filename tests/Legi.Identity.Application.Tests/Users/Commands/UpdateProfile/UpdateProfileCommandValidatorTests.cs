using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Commands.UpdateProfile;

namespace Legi.Identity.Application.Tests.Users.Commands.UpdateProfile;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = UpdateProfileCommandFactory.Create(userId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "UserId is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsTooShort()
    {
        // Arrange
        var command = UpdateProfileCommandFactory.Create(name: "A");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Name must be at least 2 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenBioIsTooLong()
    {
        // Arrange
        var command = UpdateProfileCommandFactory.Create(bio: new string('a', 501));

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Bio must be at most 500 characters");
    }

    [Fact]
    public void Validate_ShouldPass_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var command = UpdateProfileCommandFactory.Create(name: null, bio: null, avatarUrl: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
