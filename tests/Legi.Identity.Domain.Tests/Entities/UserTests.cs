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

        // Act
        var user = User.Create(email, username, passwordHash);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(username, user.Username);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.True(user.IsPublicProfile);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Create_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange & Act
        var user = UserFactory.Create();

        // Assert
        Assert.Single(user.DomainEvents);
        var domainEvent = Assert.IsType<UserRegisteredDomainEvent>(user.DomainEvents.First());
        Assert.Equal(user.Id, domainEvent.UserId);
        Assert.Equal(user.Email.Value, domainEvent.Email);
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

        for (int i = 0; i < 5; i++)
            user.AddRefreshToken($"token_{i}", expiresAt);

        // Act - Add 6th token
        user.AddRefreshToken("token_6", expiresAt);

        // Assert
        Assert.Equal(5, user.RefreshTokens.Count(t => t.IsActive));
        Assert.Equal(6, user.RefreshTokens.Count);
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
    public void UpdateProfile_ShouldUpdateIsPublicProfile()
    {
        // Arrange
        var user = CreateValidUser();
        Assert.True(user.IsPublicProfile);

        // Act
        user.UpdateProfile(false);

        // Assert
        Assert.False(user.IsPublicProfile);
    }

    [Fact]
    public void UpdateProfile_ShouldRaiseUserProfileUpdatedEvent()
    {
        // Arrange
        var user = CreateValidUser();
        user.ClearDomainEvents();

        // Act
        user.UpdateProfile(false);

        // Assert
        Assert.Single(user.DomainEvents);
        var domainEvent = Assert.IsType<UserProfileUpdatedDomainEvent>(user.DomainEvents.First());
        Assert.False(domainEvent.IsPublicProfile);
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
