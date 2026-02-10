using Legi.Identity.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.ValueObjects;

public class UsernameTests
{
    [Theory]
    [InlineData("testusr")]
    [InlineData("john_123")]
    [InlineData("a123")]
    public void Create_ShouldAcceptValidUsernames(string value)
    {
        // Act
        var username = Username.Create(value);

        // Assert
        Assert.Equal(value.ToLowerInvariant(), username.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ShouldThrowExceptionForEmptyUsername(string value)
    {
        // Act
        var act = () => Username.Create(value);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Username is required", exception.Message);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("1username")]
    [InlineData("user-name")]
    public void Create_ShouldThrowExceptionForInvalidUsername(string value)
    {
        // Act
        var act = () => Username.Create(value);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Create_ShouldNormalizeUsernameToLowercase()
    {
        // Act
        var username = Username.Create("Test_User");

        // Assert
        Assert.Equal("test_user", username.Value);
    }
}
