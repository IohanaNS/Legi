using System.Text;
using Legi.Messaging.RabbitMq;

namespace Legi.Messaging.Tests;

public class ConsumerRetryPolicyTests
{
    private const int MaxConsumer = 5;
    private const int MaxTransient = 50;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Decide_Generic_BelowCap_Retries(long prior)
    {
        Assert.Equal(RetryDecision.Retry,
            ConsumerRetryPolicy.Decide(isTransient: false, prior, MaxConsumer, MaxTransient));
    }

    [Fact]
    public void Decide_Generic_AtCap_Parks()
    {
        // prior=4 → this is attempt 5 → reaches MaxConsumer (5) → park
        Assert.Equal(RetryDecision.Park,
            ConsumerRetryPolicy.Decide(isTransient: false, priorAttempts: 4, MaxConsumer, MaxTransient));
    }

    [Fact]
    public void Decide_Transient_GetsGenerousBudget_NotParkedAtGenericCap()
    {
        // At prior=4 a generic failure would park, but a transient one keeps retrying.
        Assert.Equal(RetryDecision.Retry,
            ConsumerRetryPolicy.Decide(isTransient: true, priorAttempts: 4, MaxConsumer, MaxTransient));
    }

    [Fact]
    public void Decide_Transient_AtTransientCap_Parks()
    {
        // prior=49 → attempt 50 → reaches MaxTransient → park even a "transient"
        Assert.Equal(RetryDecision.Park,
            ConsumerRetryPolicy.Decide(isTransient: true, priorAttempts: 49, MaxConsumer, MaxTransient));
    }

    [Fact]
    public void GetRejectedDeathCount_NullHeaders_ReturnsZero()
        => Assert.Equal(0, ConsumerRetryPolicy.GetRejectedDeathCount(null));

    [Fact]
    public void GetRejectedDeathCount_NoXDeath_ReturnsZero()
        => Assert.Equal(0, ConsumerRetryPolicy.GetRejectedDeathCount(
            new Dictionary<string, object?> { ["other"] = "x" }));

    [Fact]
    public void GetRejectedDeathCount_ReadsRejectedEntryCount_StringReason()
    {
        var headers = new Dictionary<string, object?>
        {
            ["x-death"] = new List<object>
            {
                new Dictionary<string, object?> { ["reason"] = "rejected", ["count"] = 3L },
                new Dictionary<string, object?> { ["reason"] = "expired", ["count"] = 3L }
            }
        };
        Assert.Equal(3, ConsumerRetryPolicy.GetRejectedDeathCount(headers));
    }

    [Fact]
    public void GetRejectedDeathCount_HandlesByteArrayReason_AsRabbitMqEncodesIt()
    {
        var headers = new Dictionary<string, object?>
        {
            ["x-death"] = new List<object>
            {
                new Dictionary<string, object?> { ["reason"] = Encoding.UTF8.GetBytes("rejected"), ["count"] = 7L }
            }
        };
        Assert.Equal(7, ConsumerRetryPolicy.GetRejectedDeathCount(headers));
    }

    [Fact]
    public void GetRejectedDeathCount_OnlyExpiredEntry_ReturnsZero()
    {
        var headers = new Dictionary<string, object?>
        {
            ["x-death"] = new List<object>
            {
                new Dictionary<string, object?> { ["reason"] = "expired", ["count"] = 4L }
            }
        };
        Assert.Equal(0, ConsumerRetryPolicy.GetRejectedDeathCount(headers));
    }
}
