namespace Legi.Catalog.Infrastructure.Persistence.Entities;

public enum CoverIngestionJobStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,

    /// <summary>
    /// Terminal: we ran the bounded retry window and never found a real cover.
    /// Scheduled polling stops (the row drains out of the active set); the
    /// placeholder is the resting state. Late covers are still picked up by free
    /// opportunistic re-checks (external-search enrich) and manual upload.
    /// </summary>
    Exhausted = 3
}

/// <summary>
/// A durable "this edition still needs a cover" row. Mirrors
/// <see cref="ExternalBookSearchJobEntity"/> (claim with FOR UPDATE SKIP LOCKED +
/// decaying backoff). Enqueued when a book is imported cover-less; the worker
/// re-probes the providers on a decaying cadence until it finds a real cover or
/// gives up.
///
/// The give-up budget is "N <em>confirmed</em> no-cover results", not "N
/// attempts": a provider outage reschedules without counting, so one bad day
/// doesn't burn every book's budget (locked decision — don't penalize outages).
/// </summary>
public class CoverIngestionJobEntity
{
    public Guid Id { get; set; }

    /// <summary>The edition (book) that needs a cover. Covers attach at edition level.</summary>
    public Guid BookId { get; set; }

    /// <summary>The ISBN, used to build candidate cover URLs and the blob key.</summary>
    public string Isbn { get; set; } = null!;

    public CoverIngestionJobStatus Status { get; set; }

    /// <summary>
    /// Successful probes that confirmed no cover exists. Counts toward
    /// <see cref="CoverIngestionJobStatus.Exhausted"/>; drives the decaying cadence.
    /// </summary>
    public int NoCoverAttempts { get; set; }

    /// <summary>
    /// Provider-unreachable / timeout probes. Informational — these reschedule with
    /// a short backoff but do <em>not</em> count toward the give-up budget.
    /// </summary>
    public int TransientFailures { get; set; }

    public DateTime NextRetryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
