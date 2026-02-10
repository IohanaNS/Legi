using Legi.Identity.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.Entities;

public class UserRefreshTokenTests
{
    [Fact]
    public void AddRefreshToken_ShouldCreateInactiveToken_WhenTokenIsAlreadyExpired()
    {
        // Arrange
        var user = UserFactory.Create();
        var expiresAt = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var token = user.AddRefreshToken("expired_token", expiresAt);

        // Assert
        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
        Assert.Null(user.GetValidRefreshToken("expired_token"));
    }

    [Fact]
    public void RevokeRefreshToken_ShouldThrowException_WhenTokenWasAlreadyRevoked()
    {
        // Arrange
        var user = UserFactory.Create();
        user.AddRefreshToken("revoked_token", DateTime.UtcNow.AddDays(1));
        user.RevokeRefreshToken("revoked_token");

        // Act
        var act = () => user.RevokeRefreshToken("revoked_token");

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Token has already been revoked", exception.Message);
    }

    [Fact]
    public void GetValidRefreshToken_ShouldReturnNull_WhenTokenIsExpired()
    {
        // Arrange
        var user = UserFactory.Create();
        user.AddRefreshToken("expired_lookup_token", DateTime.UtcNow.AddMinutes(-10));

        // Act
        var result = user.GetValidRefreshToken("expired_lookup_token");

        // Assert
        Assert.Null(result);
    }
}
