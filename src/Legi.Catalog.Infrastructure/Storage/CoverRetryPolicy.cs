namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// The bounded, decaying retry schedule for cover discovery. Keyed on the number
/// of <em>confirmed</em> no-cover probes recorded so far (transient failures don't
/// count, so an outage can't burn the budget). After
/// <see cref="MaxNoCoverAttempts"/> confirmed misses the job is Exhausted and
/// scheduled polling stops.
/// </summary>
internal static class CoverRetryPolicy
{
    /// <summary>Confirmed no-cover probes before giving up (→ Exhausted).</summary>
    public const int MaxNoCoverAttempts = 5;

    // Delay before the probe numbered (index + 1). Index 0 is the delay set at
    // enqueue (the inline attempt just failed, so don't re-probe immediately).
    private static readonly TimeSpan[] Schedule =
    [
        TimeSpan.FromHours(1),  // before probe #1
        TimeSpan.FromHours(6),  // before probe #2
        TimeSpan.FromDays(1),   // before probe #3
        TimeSpan.FromDays(3),   // before probe #4
        TimeSpan.FromDays(7),   // before probe #5
    ];

    /// <summary>Backoff for a transient (provider-unreachable) probe — short, and budget-free.</summary>
    public static readonly TimeSpan TransientBackoff = TimeSpan.FromMinutes(30);

    public static bool IsExhausted(int noCoverAttempts) => noCoverAttempts >= MaxNoCoverAttempts;

    /// <summary>Delay until the next probe given the confirmed-no-cover count recorded so far.</summary>
    public static TimeSpan NextDelay(int noCoverAttempts)
    {
        var index = Math.Clamp(noCoverAttempts, 0, Schedule.Length - 1);
        return Schedule[index];
    }
}
