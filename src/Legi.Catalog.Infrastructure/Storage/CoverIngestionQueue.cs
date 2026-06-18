using Legi.Catalog.Application.Common.Storage;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// Adds a Pending cover-discovery row, deduped per book against the partial unique
/// index on active jobs (so re-imports don't pile up duplicates). The first probe
/// is scheduled one cadence out, not immediately — the inline acquire just failed.
/// </summary>
public sealed class CoverIngestionQueue(
    CatalogDbContext context,
    ILogger<CoverIngestionQueue> logger)
    : ICoverIngestionQueue
{
    public async Task EnqueueAsync(Guid bookId, string isbn, CancellationToken cancellationToken)
    {
        var activeJobExists = await context.CoverIngestionJobs
            .AsNoTracking()
            .AnyAsync(
                j => j.BookId == bookId
                     && (j.Status == CoverIngestionJobStatus.Pending
                         || j.Status == CoverIngestionJobStatus.Processing),
                cancellationToken);

        if (activeJobExists)
            return;

        var now = DateTime.UtcNow;
        context.CoverIngestionJobs.Add(new CoverIngestionJobEntity
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Isbn = isbn,
            Status = CoverIngestionJobStatus.Pending,
            NoCoverAttempts = 0,
            TransientFailures = 0,
            NextRetryAt = now.Add(CoverRetryPolicy.NextDelay(0)),
            CreatedAt = now
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A job for this book was enqueued concurrently — treat as a no-op.
            logger.LogDebug(ex, "Cover ingestion job for book {BookId} was queued concurrently", bookId);
            foreach (var entry in context.ChangeTracker.Entries<CoverIngestionJobEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.State = EntityState.Detached;
            }
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
