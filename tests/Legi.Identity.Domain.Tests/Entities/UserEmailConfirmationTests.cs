using Legi.Identity.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.Entities;

public class UserEmailConfirmationTests
{
    [Fact]
    public void Create_ShouldLeaveEmailUnconfirmed()
    {
        // Arrange & Act
        var user = UserFactory.Create();

        // Assert
        Assert.False(user.IsEmailConfirmed);
        Assert.Null(user.EmailConfirmedAt);
    }

    [Fact]
    public void AddEmailConfirmationToken_ShouldCreateActiveToken()
    {
        // Arrange
        var user = UserFactory.Create();

        // Act
        var token = user.AddEmailConfirmationToken("hash_1", DateTime.UtcNow.AddHours(1));

        // Assert
        Assert.True(token.IsActive);
        Assert.False(token.IsUsed);
        Assert.False(token.IsExpired);
        Assert.Single(user.EmailConfirmationTokens);
    }

    [Fact]
    public void AddEmailConfirmationToken_ShouldInvalidatePriorActiveTokens()
    {
        // Arrange
        var user = UserFactory.Create();
        var first = user.AddEmailConfirmationToken("hash_old", DateTime.UtcNow.AddHours(1));

        // Act
        var second = user.AddEmailConfirmationToken("hash_new", DateTime.UtcNow.AddHours(1));

        // Assert
        Assert.True(first.IsUsed);
        Assert.False(first.IsActive);
        Assert.True(second.IsActive);
    }

    [Fact]
    public void ConfirmEmail_ShouldSetEmailConfirmedAtAndMarkTokenUsed_OnSuccess()
    {
        // Arrange
        var user = UserFactory.Create();
        var now = new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        user.AddEmailConfirmationToken("confirm_hash", now.AddHours(1));

        // Act
        user.ConfirmEmail("confirm_hash", now);

        // Assert
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(now, user.EmailConfirmedAt);
        Assert.True(user.EmailConfirmationTokens.Single().IsUsed);
    }

    [Fact]
    public void MarkEmailConfirmationTokenSent_ShouldSetSentAt()
    {
        // Arrange
        var user = UserFactory.Create();
        var now = new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        user.AddEmailConfirmationToken("confirm_hash", now.AddHours(1));

        // Act
        user.MarkEmailConfirmationTokenSent("confirm_hash", now);

        // Assert
        Assert.Equal(now, user.EmailConfirmationTokens.Single().SentAt);
    }

    [Fact]
    public void MarkEmailConfirmationTokenSent_ShouldThrow_WhenTokenIsUnknown()
    {
        // Arrange
        var user = UserFactory.Create();

        // Act
        var act = () => user.MarkEmailConfirmationTokenSent("missing_hash", DateTime.UtcNow);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Confirmation token not found", exception.Message);
    }

    [Fact]
    public void ConfirmEmail_ShouldThrow_WhenTokenIsUnknown()
    {
        // Arrange
        var user = UserFactory.Create();
        user.AddEmailConfirmationToken("confirm_hash", DateTime.UtcNow.AddHours(1));

        // Act
        var act = () => user.ConfirmEmail("wrong_hash", DateTime.UtcNow);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Invalid or expired email confirmation token", exception.Message);
    }

    [Fact]
    public void ConfirmEmail_ShouldThrow_WhenTokenIsExpired()
    {
        // Arrange
        var user = UserFactory.Create();
        var now = new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        user.AddEmailConfirmationToken("confirm_hash", now.AddMinutes(-1));

        // Act
        var act = () => user.ConfirmEmail("confirm_hash", now);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void ConfirmEmail_ShouldThrow_WhenTokenAlreadyUsed()
    {
        // Arrange
        var user = UserFactory.Create();
        var now = new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        user.AddEmailConfirmationToken("confirm_hash", now.AddHours(1));
        user.ConfirmEmail("confirm_hash", now);

        // Act
        var act = () => user.ConfirmEmail("confirm_hash", now.AddMinutes(1));

        // Assert
        Assert.Throws<DomainException>(act);
    }
}
