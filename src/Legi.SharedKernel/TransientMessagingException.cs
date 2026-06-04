namespace Legi.SharedKernel;

/// <summary>
/// Thrown by an integration-event handler to signal that the failure is
/// <b>transient</b> — the message should be retried because the condition is
/// expected to resolve on its own (e.g. a referenced local read-model/snapshot
/// that another, not-yet-consumed event will create).
///
/// The consumer host treats this differently from a generic exception: transient
/// failures get a generous retry budget (they <i>should</i> keep retrying until
/// the prerequisite arrives), whereas a generic exception is treated as probable
/// poison and parked after a small number of attempts. See
/// MESSAGING-ARCHITECTURE-decisions.md, decisions 8.3 and Fase 6 (6B).
///
/// Use this only for genuinely self-resolving conditions (the §8.3 "snapshot not
/// yet arrived" / "book not yet created" cases). A bug or corrupt data is NOT
/// transient — let it throw a normal exception so it parks fast.
/// </summary>
public class TransientMessagingException : Exception
{
    public TransientMessagingException(string message) : base(message)
    {
    }

    public TransientMessagingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
