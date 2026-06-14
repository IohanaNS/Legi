using Legi.Identity.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.Entities;

public class UserPasswordResetTests
{
    [Fact]
    public void AddPasswordResetToken_ShouldCreateActiveToken()
    {
        // Arrange
        var user = UserFactory.Create();

        // Act
        var token = user.AddPasswordResetToken("hash_1", DateTime.UtcNow.AddHours(1));

        // Assert
        Assert.True(token.IsActive);
        Assert.False(token.IsUsed);
        Assert.False(token.IsExpired);
        Assert.Single(user.PasswordResetTokens);
    }

    [Fact]
    public void AddPasswordResetToken_ShouldInvalidatePriorActiveTokens()
    {
        // Arrange
        var user = UserFactory.Create();
        var first = user.AddPasswordResetToken("hash_old", DateTime.UtcNow.AddHours(1));

        // Act
        var second = user.AddPasswordResetToken("hash_new", DateTime.UtcNow.AddHours(1));

        // Assert
        Assert.True(first.IsUsed);
        Assert.False(first.IsActive);
        Assert.True(second.IsActive);
    }

    [Fact]
    public void RedeemPasswordReset_ShouldChangePasswordAndRevokeRefreshTokens_OnSuccess()
    {
        // Arrange
        var user = UserFactory.Create(passwordHash: "old_hash");
        user.AddRefreshToken("refresh_hash", DateTime.UtcNow.AddDays(7));
        user.AddPasswordResetToken("reset_hash", DateTime.UtcNow.AddHours(1));

        // Act
        user.RedeemPasswordReset("reset_hash", "new_hash", DateTime.UtcNow);

        // Assert
        Assert.Equal("new_hash", user.PasswordHash);
        Assert.Null(user.GetValidRefreshToken("refresh_hash"));
        Assert.True(user.PasswordResetTokens.Single().IsUsed);
    }

    [Fact]
    public void RedeemPasswordReset_ShouldThrow_WhenTokenIsUnknown()
    {
        // Arrange
        var user = UserFactory.Create();
        user.AddPasswordResetToken("reset_hash", DateTime.UtcNow.AddHours(1));

        // Act
        var act = () => user.RedeemPasswordReset("wrong_hash", "new_hash", DateTime.UtcNow);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Invalid or expired reset token", exception.Message);
    }

    [Fact]
    public void RedeemPasswordReset_ShouldThrow_WhenTokenIsExpired()
    {
        // Arrange
        var user = UserFactory.Create();
        user.AddPasswordResetToken("reset_hash", DateTime.UtcNow.AddMinutes(-1));

        // Act
        var act = () => user.RedeemPasswordReset("reset_hash", "new_hash", DateTime.UtcNow);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void RedeemPasswordReset_ShouldThrow_WhenTokenAlreadyUsed()
    {
        // Arrange
        var user = UserFactory.Create();
        user.AddPasswordResetToken("reset_hash", DateTime.UtcNow.AddHours(1));
        user.RedeemPasswordReset("reset_hash", "new_hash", DateTime.UtcNow);

        // Act
        var act = () => user.RedeemPasswordReset("reset_hash", "newer_hash", DateTime.UtcNow);

        // Assert
        Assert.Throws<DomainException>(act);
    }
}
