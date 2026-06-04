using System.Text;

namespace Legi.Messaging.RabbitMq;

/// <summary>
/// What the consumer host should do with a message after a processing failure.
/// </summary>
public enum RetryDecision
{
    /// <summary>Dead-letter into the retry queue (will be redelivered after the TTL).</summary>
    Retry,

    /// <summary>Budget exhausted — divert to the parking lot (error queue), terminal.</summary>
    Park
}

/// <summary>
/// Pure, broker-free policy for the consumer retry/parking decision (Fase 6
/// 6A/6B). Separated from <c>RabbitMqConsumerHost</c> so the transient-vs-poison
/// budgets and the <c>x-death</c> attempt-count parsing are unit-testable without
/// a live RabbitMQ.
/// </summary>
public static class ConsumerRetryPolicy
{
    /// <summary>
    /// Decides retry-vs-park given whether the failure was transient, how many
    /// times the message has already been rejected from the work queue
    /// (<paramref name="priorAttempts"/>), and the two budgets.
    ///
    /// <paramref name="priorAttempts"/> is the count BEFORE this failure (0 on the
    /// first delivery). The message is parked once it has used up its budget — i.e.
    /// this failure would be attempt number <c>priorAttempts + 1</c>; if that
    /// reaches the cap, park.
    /// </summary>
    public static RetryDecision Decide(
        bool isTransient, long priorAttempts, int maxConsumerAttempts, int maxTransientAttempts)
    {
        var cap = isTransient ? maxTransientAttempts : maxConsumerAttempts;
        // priorAttempts counts failures already dead-lettered; +1 for the current one.
        return priorAttempts + 1 >= cap ? RetryDecision.Park : RetryDecision.Retry;
    }

    /// <summary>
    /// Reads how many times this message has been rejected from the work queue,
    /// from the RabbitMQ <c>x-death</c> header. Returns 0 when absent (first
    /// delivery). The header is a list of death records; the entry with
    /// reason "rejected" carries the work-queue rejection count.
    /// </summary>
    public static long GetRejectedDeathCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-death", out var raw) || raw is not IEnumerable<object> entries)
            return 0;

        foreach (var entry in entries)
        {
            if (entry is not IDictionary<string, object?> record)
                continue;

            if (!record.TryGetValue("reason", out var reasonObj) || AsString(reasonObj) != "rejected")
                continue;

            if (record.TryGetValue("count", out var countObj) && countObj is long count)
                return count;
        }

        return 0;
    }

    private static string? AsString(object? value) => value switch
    {
        null => null,
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        string s => s,
        _ => value.ToString()
    };
}
