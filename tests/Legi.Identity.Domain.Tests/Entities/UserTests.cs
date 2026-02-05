using FluentAssertions;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.ValueObjects;
using Legi.Identity.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_ShouldCreateUserWithValidData()
    {
        // Arrange
        var email = EmailFactory.Create();
        var username = UsernameFactory.Create();
        var passwordHash = "hashed_password";
        var name = "Test User";

        // Act
        var user = User.Create(email, username, passwordHash, name);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
        user.Username.Should().Be(username);
        user.PasswordHash.Should().Be(passwordHash);
        user.Name.Should().Be(name);
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange & Act
        var user = UserFactory.Create(name: "Test");

        // Assert
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<UserRegisteredDomainEvent>();

        var domainEvent = user.DomainEvents.First() as UserRegisteredDomainEvent;
        domainEvent!.UserId.Should().Be(user.Id);
        domainEvent.Email.Should().Be(user.Email.Value);
        domainEvent.Name.Should().Be("Test");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")] // Too short
    public void Create_ShouldThrowExceptionForInvalidName(string invalidName)
    {
        // Arrange
        var email = EmailFactory.Create();
        var username = UsernameFactory.Create();

        // Act
        var act = () => User.Create(email, username, "hash", invalidName);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldThrowExceptionForNameTooLong()
    {
        // Arrange
        var longName = new string('A', 101); // 101 characters

        // Act
        var act = () => UserFactory.Create(name: longName);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Name must be between 2 and 100 characters");
    }

    [Fact]
    public void AddRefreshToken_ShouldAddTokenToUser()
    {
        // Arrange
        var user = CreateValidUser();
        var tokenHash = "token_hash";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var token = user.AddRefreshToken(tokenHash, expiresAt);

        // Assert
        token.Should().NotBeNull();
        token.TokenHash.Should().Be(tokenHash);
        token.ExpiresAt.Should().Be(expiresAt);
        user.RefreshTokens.Should().Contain(token);
    }

    [Fact]
    public void AddRefreshToken_ShouldRevokeOldestTokenWhenLimitExceeded()
    {
        // Arrange
        var user = CreateValidUser();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Add 5 tokens (limit)
        for (int i = 0; i < 5; i++)
        {
            user.AddRefreshToken($"token_{i}", expiresAt);
        }

        // Act - Add 6th token
        user.AddRefreshToken("token_6", expiresAt);

        // Assert
        user.RefreshTokens.Count(t => t.IsActive).Should().Be(5);
        user.RefreshTokens.Should().HaveCount(6); // Total includes revoked
    }

    [Fact]
    public void RevokeRefreshToken_ShouldRevokeSpecificToken()
    {
        // Arrange
        var user = CreateValidUser();
        var tokenHash = "token_hash";
        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(7));

        // Act
        user.RevokeRefreshToken(tokenHash);

        // Assert
        var token = user.RefreshTokens.First(t => t.TokenHash == tokenHash);
        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void RevokeRefreshToken_ShouldThrowExceptionWhenTokenNotFound()
    {
        // Arrange
        var user = CreateValidUser();

        // Act
        var act = () => user.RevokeRefreshToken("non_existent_token");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Token not found");
    }

    [Fact]
    public void RevokeAllRefreshTokens_ShouldRevokeAllActiveTokens()
    {
        // Arrange
        var user = CreateValidUser();
        user.AddRefreshToken("token_1", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken("token_2", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken("token_3", DateTime.UtcNow.AddDays(7));

        // Act
        user.RevokeAllRefreshTokens();

        // Assert
        user.RefreshTokens.Should().AllSatisfy(t => t.IsActive.Should().BeFalse());
    }

    [Fact]
    public void GetValidRefreshToken_ShouldReturnActiveValidToken()
    {
        // Arrange
        var user = CreateValidUser();
        var tokenHash = "valid_token";
        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(7));

        // Act
        var token = user.GetValidRefreshToken(tokenHash);

        // Assert
        token.Should().NotBeNull();
        token!.TokenHash.Should().Be(tokenHash);
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void GetValidRefreshToken_ShouldReturnNullForRevokedToken()
    {
        // Arrange
        var user = CreateValidUser();
        var tokenHash = "token_hash";
        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(7));
        user.RevokeRefreshToken(tokenHash);

        // Act
        var token = user.GetValidRefreshToken(tokenHash);

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateUserData()
    {
        // Arrange
        var user = CreateValidUser();
        var newName = "Updated Name";
        var newBio = "New user bio";
        var newAvatar = "https://example.com/avatar.jpg";

        // Act
        user.UpdateProfile(newName, newBio, newAvatar);

        // Assert
        user.Name.Should().Be(newName);
        user.Bio.Should().Be(newBio);
        user.AvatarUrl.Should().Be(newAvatar);
    }

    [Fact]
    public void UpdateProfile_ShouldRaiseUserProfileUpdatedEvent()
    {
        // Arrange
        var user = CreateValidUser();
        user.ClearDomainEvents(); // Clear creation event

        // Act
        user.UpdateProfile("New Name", "New Bio", "https://avatar.com/img.jpg");

        // Assert
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<UserProfileUpdatedDomainEvent>();
    }

    [Fact]
    public void UpdateProfile_ShouldThrowExceptionForBioTooLong()
    {
        // Arrange
        var user = CreateValidUser();
        var longBio = new string('A', 501); // 501 characters

        // Act
        var act = () => user.UpdateProfile(null, longBio, null);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Bio must be at most 500 characters");
    }

    [Fact]
    public void UpdatePassword_ShouldUpdatePasswordHash()
    {
        // Arrange
        var user = CreateValidUser();
        var newHash = "new_password_hash";

        // Act
        user.UpdatePassword(newHash);

        // Assert
        user.PasswordHash.Should().Be(newHash);
    }

    [Fact]
    public void UpdatePassword_ShouldRevokeAllRefreshTokens()
    {
        // Arrange
        var user = CreateValidUser();
        user.AddRefreshToken("token_1", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken("token_2", DateTime.UtcNow.AddDays(7));

        // Act
        user.UpdatePassword("new_hash");

        // Assert
        user.RefreshTokens.Should().AllSatisfy(t => t.IsActive.Should().BeFalse());
    }

    // Helper method
    private User CreateValidUser() => UserFactory.Create();
}
