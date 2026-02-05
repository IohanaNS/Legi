using FluentAssertions;
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
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowExceptionForEmptyEmail(string emptyEmail)
    {
        // Act
        var act = () => Email.Create(emptyEmail);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Email is required");
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
        act.Should().Throw<DomainException>()
           .WithMessage("Invalid email format");
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowercase()
    {
        // Arrange
        var uppercaseEmail = "TEST@EXAMPLE.COM";

        // Act
        var email = Email.Create(uppercaseEmail);

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void TwoEmailsWithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        // Act & Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void TwoEmailsWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }
}
