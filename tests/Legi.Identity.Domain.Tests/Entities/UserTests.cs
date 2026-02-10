using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Events;
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
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(username, user.Username);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(name, user.Name);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Create_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange & Act
        var user = UserFactory.Create(name: "Test");

        // Assert
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRegisteredDomainEvent>(user.DomainEvents.First());

        var domainEvent = Assert.IsType<UserRegisteredDomainEvent>(user.DomainEvents.First());
        Assert.Equal(user.Id, domainEvent.UserId);
        Assert.Equal(user.Email.Value, domainEvent.Email);
        Assert.Equal("Test", domainEvent.Name);
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
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Create_ShouldThrowExceptionForNameTooLong()
    {
        // Arrange
        var longName = new string('A', 101); // 101 characters

        // Act
        var act = () => UserFactory.Create(name: longName);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Name must be between 2 and 100 characters", exception.Message);
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
        Assert.NotNull(token);
        Assert.Equal(tokenHash, token.TokenHash);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Contains(token, user.RefreshTokens);
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
        Assert.Equal(5, user.RefreshTokens.Count(t => t.IsActive));
        Assert.Equal(6, user.RefreshTokens.Count); // Total includes revoked
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
        Assert.False(token.IsActive);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public void RevokeRefreshToken_ShouldThrowExceptionWhenTokenNotFound()
    {
        // Arrange
        var user = CreateValidUser();

        // Act
        var act = () => user.RevokeRefreshToken("non_existent_token");

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Token not found", exception.Message);
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
        Assert.All(user.RefreshTokens, t => Assert.False(t.IsActive));
    }

    [Fact]
    public void GetValidRefreshToken_ShouldReturnActiveValidToken()
    {
        // Arrange
        var user = CreateValidUser();
        const string tokenHash = "valid_token";
        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(7));

        // Act
        var token = user.GetValidRefreshToken(tokenHash);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(tokenHash, token.TokenHash);
        Assert.True(token.IsActive);
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
        Assert.Null(token);
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
        Assert.Equal(newName, user.Name);
        Assert.Equal(newBio, user.Bio);
        Assert.Equal(newAvatar, user.AvatarUrl);
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
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserProfileUpdatedDomainEvent>(user.DomainEvents.First());
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
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Bio must be at most 500 characters", exception.Message);
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
        Assert.Equal(newHash, user.PasswordHash);
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
        Assert.All(user.RefreshTokens, t => Assert.False(t.IsActive));
    }

    // Helper method
    private User CreateValidUser() => UserFactory.Create();
}
