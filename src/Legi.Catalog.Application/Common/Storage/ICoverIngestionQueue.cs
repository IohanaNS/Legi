namespace Legi.Catalog.Application.Common.Storage;

/// <summary>
/// Enqueues a durable "this edition needs a cover" job so a background worker can
/// re-probe the providers on a decaying cadence. Called when a book is imported
/// cover-less (the inline acquire found nothing) — the safety net so a transient
/// miss isn't permanent. Idempotent per book: a second enqueue while a job is
/// already active is a no-op.
/// </summary>
public interface ICoverIngestionQueue
{
    Task EnqueueAsync(Guid bookId, string isbn, CancellationToken cancellationToken);
}
