using Legi.Identity.Domain.Entities;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.Entities;

public class MfaEmailCodeTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Issue_CreatesUsableCode()
    {
        var now = DateTime.UtcNow;

        var code = MfaEmailCode.Issue(UserId, "hash", now.AddMinutes(10), now);

        Assert.Equal(UserId, code.UserId);
        Assert.Equal("hash", code.CodeHash);
        Assert.Equal(0, code.AttemptCount);
        Assert.False(code.IsConsumed);
        Assert.True(code.IsUsable(now));
    }

    [Fact]
    public void Issue_Throws_WhenExpiryNotInFuture()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<DomainException>(() => MfaEmailCode.Issue(UserId, "hash", now, now));
    }

    [Fact]
    public void IsUsable_False_AfterExpiry()
    {
        var now = DateTime.UtcNow;
        var code = MfaEmailCode.Issue(UserId, "hash", now.AddMinutes(10), now);

        Assert.False(code.IsUsable(now.AddMinutes(11)));
    }

    [Fact]
    public void IsUsable_False_WhenAttemptsExhausted()
    {
        var now = DateTime.UtcNow;
        var code = MfaEmailCode.Issue(UserId, "hash", now.AddMinutes(10), now);

        for (var i = 0; i < MfaEmailCode.MaxAttempts; i++)
            code.RegisterFailedAttempt(now);

        Assert.Equal(MfaEmailCode.MaxAttempts, code.AttemptCount);
        Assert.False(code.IsUsable(now));
    }

    [Fact]
    public void IsUsable_False_AfterConsume()
    {
        var now = DateTime.UtcNow;
        var code = MfaEmailCode.Issue(UserId, "hash", now.AddMinutes(10), now);

        code.Consume(now);

        Assert.True(code.IsConsumed);
        Assert.False(code.IsUsable(now));
    }
}
