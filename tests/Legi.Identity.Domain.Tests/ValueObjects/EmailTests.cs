using Legi.Identity.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@company.com.br")]
    [InlineData("email+tag@domain.co")]
    public void Create_ShouldAcceptValidEmails(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);

        // Assert
        Assert.NotNull(email);
        Assert.Equal(validEmail.ToLowerInvariant(), email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowExceptionForEmptyEmail(string emptyEmail)
    {
        // Act
        var act = () => Email.Create(emptyEmail);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Email is required", exception.Message);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user @domain.com")]
    public void Create_ShouldThrowExceptionForInvalidFormat(string invalidEmail)
    {
        // Act
        var act = () => Email.Create(invalidEmail);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Invalid email format", exception.Message);
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowercase()
    {
        // Arrange
        var uppercaseEmail = "TEST@EXAMPLE.COM";

        // Act
        var email = Email.Create(uppercaseEmail);

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void TwoEmailsWithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        // Act & Assert
        Assert.Equal(email1, email2);
        Assert.True(email1 == email2);
    }

    [Fact]
    public void TwoEmailsWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        Assert.NotEqual(email1, email2);
        Assert.True(email1 != email2);
    }
}
