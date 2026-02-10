using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Commands.DeleteAccount;

namespace Legi.Identity.Application.Tests.Users.Commands.DeleteAccount;

public class DeleteAccountCommandValidatorTests
{
    private readonly DeleteAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = DeleteAccountCommandFactory.Create(Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "UserId is required");
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = DeleteAccountCommandFactory.Create();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
