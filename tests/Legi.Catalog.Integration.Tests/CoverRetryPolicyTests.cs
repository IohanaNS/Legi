using Legi.Catalog.Infrastructure.Storage;

namespace Legi.Catalog.Integration.Tests;

/// <summary>
/// Pure unit tests (no DB) for the cover-discovery retry budget and decaying
/// cadence. The key invariant: the give-up budget is in <em>confirmed no-cover</em>
/// probes, and the cadence decays so a cover-less book is probed a handful of
/// times over ~weeks, then goes quiet.
/// </summary>
public class CoverRetryPolicyTests
{
    [Fact]
    public void Cadence_Decays_AcrossTheBudget()
    {
        // index = confirmed no-cover attempts recorded so far.
        Assert.Equal(TimeSpan.FromHours(1), CoverRetryPolicy.NextDelay(0)); // first scheduled probe
        Assert.Equal(TimeSpan.FromHours(6), CoverRetryPolicy.NextDelay(1));
        Assert.Equal(TimeSpan.FromDays(1), CoverRetryPolicy.NextDelay(2));
        Assert.Equal(TimeSpan.FromDays(3), CoverRetryPolicy.NextDelay(3));
        Assert.Equal(TimeSpan.FromDays(7), CoverRetryPolicy.NextDelay(4));
    }

    [Fact]
    public void NextDelay_Saturates_BeyondSchedule()
    {
        // Defensive: never throw if asked past the last bucket.
        Assert.Equal(TimeSpan.FromDays(7), CoverRetryPolicy.NextDelay(99));
    }

    [Fact]
    public void IsExhausted_OnlyAfterTheConfirmedBudgetIsSpent()
    {
        for (var attempts = 0; attempts < CoverRetryPolicy.MaxNoCoverAttempts; attempts++)
            Assert.False(CoverRetryPolicy.IsExhausted(attempts));

        Assert.True(CoverRetryPolicy.IsExhausted(CoverRetryPolicy.MaxNoCoverAttempts));
        Assert.True(CoverRetryPolicy.IsExhausted(CoverRetryPolicy.MaxNoCoverAttempts + 1));
    }

    [Fact]
    public void TransientBackoff_IsShort_AndDoesNotConsumeBudget()
    {
        // A provider outage reschedules soon; it's not part of the decaying ladder,
        // so it can't push a book toward Exhausted (the "don't penalize outages" rule).
        Assert.True(CoverRetryPolicy.TransientBackoff < CoverRetryPolicy.NextDelay(0));
    }
}
