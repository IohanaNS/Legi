using Legi.Identity.Domain.Entities;

namespace Legi.Identity.Domain.Tests.Entities;

public class LoginAttemptTests
{
    [Fact]
    public void NormalizeIdentifier_ShouldTrimAndLowercase()
    {
        // Act
        var result = LoginAttempt.NormalizeIdentifier("  USER@Example.COM ");

        // Assert
        Assert.Equal("user@example.com", result);
    }

    [Fact]
    public void RecordFailedLogin_ShouldLockWhenLimitIsReached()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var attempt = LoginAttempt.Create("user@example.com", now);

        // Act
        attempt.RecordFailedLogin(2, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), now);
        attempt.RecordFailedLogin(2, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), now.AddMinutes(1));

        // Assert
        Assert.Equal(2, attempt.FailedAttempts);
        Assert.Equal(now.AddMinutes(11), attempt.LockoutEndsAt);
    }

    [Fact]
    public void RecordFailedLogin_ShouldResetAfterFailureWindow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var attempt = LoginAttempt.Create("user@example.com", now);

        // Act
        attempt.RecordFailedLogin(3, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), now);
        attempt.RecordFailedLogin(3, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), now.AddMinutes(16));

        // Assert
        Assert.Equal(1, attempt.FailedAttempts);
        Assert.Null(attempt.LockoutEndsAt);
    }
}
