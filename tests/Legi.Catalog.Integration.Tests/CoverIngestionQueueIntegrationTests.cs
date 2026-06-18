using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Legi.Catalog.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Legi.Catalog.Integration.Tests;

/// <summary>
/// Verifies the cover-discovery enqueue against a real Postgres: a cover-less book
/// gets one Pending job scheduled one cadence out, and a second enqueue for the
/// same book is a no-op (deduped by the partial unique index on active jobs).
///
/// Set <c>CATALOG_TEST_DB</c> to a live, migrated Catalog Postgres; skips otherwise.
/// </summary>
public class CoverIngestionQueueIntegrationTests
{
    [SkippableFact]
    public async Task EnqueueAsync_CreatesOnePendingJob_AndDedupesRepeatEnqueue()
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "CATALOG_TEST_DB not set");

        var options = new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(conn).Options;
        var bookId = Guid.NewGuid();
        var isbn = "9780000000001";
        var before = DateTime.UtcNow;

        await using (var context = new CatalogDbContext(options))
        {
            var queue = new CoverIngestionQueue(context, NullLogger<CoverIngestionQueue>.Instance);
            await queue.EnqueueAsync(bookId, isbn, CancellationToken.None);
            await queue.EnqueueAsync(bookId, isbn, CancellationToken.None); // dedup: no-op
        }

        await using (var context = new CatalogDbContext(options))
        {
            var jobs = await context.CoverIngestionJobs
                .Where(j => j.BookId == bookId)
                .ToListAsync();

            try
            {
                Assert.Single(jobs);
                var job = jobs[0];
                Assert.Equal(CoverIngestionJobStatus.Pending, job.Status);
                Assert.Equal(isbn, job.Isbn);
                Assert.Equal(0, job.NoCoverAttempts);
                // First probe is scheduled one cadence out (~1h), not immediately.
                Assert.True(job.NextRetryAt >= before.AddMinutes(50));
            }
            finally
            {
                context.CoverIngestionJobs.RemoveRange(jobs);
                await context.SaveChangesAsync();
            }
        }
    }
}
