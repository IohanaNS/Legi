using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Application.Common.Storage;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// Background discovery for cover-less editions. Claims due jobs (FOR UPDATE SKIP
/// LOCKED), re-probes the providers, and on a real cover updates the book (raising
/// <c>BookUpdated</c> so Library/Social snapshots get the blob URL). Distinguishes
/// a provider outage (reschedule, budget-free) from a confirmed no-cover (counts
/// toward Exhausted) so one bad day doesn't make every book give up early.
/// </summary>
public sealed class CoverIngestionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CoverIngestionWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    // A row stuck in Processing this long (worker crashed mid-job) is reclaimed by
    // the next poll, so a crash never strands a job. Generous vs. a normal probe.
    private static readonly TimeSpan StaleProcessingAfter = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Cover ingestion worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextJobAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cover ingestion worker poll failed");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<CatalogDbContext>();

        var job = await ClaimNextJobAsync(context, cancellationToken);
        if (job is null)
            return false;

        try
        {
            await DiscoverAsync(job, sp, context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected failure: reschedule as transient (budget-free) so a bug or
            // blip doesn't permanently strand the job or burn its give-up budget.
            logger.LogWarning(ex, "Cover ingestion job {JobId} failed unexpectedly", job.Id);
            await RescheduleTransientAsync(job.Id, ex, cancellationToken);
        }

        return true;
    }

    private async Task DiscoverAsync(
        CoverIngestionJobEntity job,
        IServiceProvider sp,
        CatalogDbContext context,
        CancellationToken cancellationToken)
    {
        var bookRepository = sp.GetRequiredService<IBookRepository>();
        var now = DateTime.UtcNow;
        job.UpdatedAt = now;

        var book = await bookRepository.GetByIdAsync(job.BookId, cancellationToken);
        if (book is null)
        {
            // Nothing to discover for a removed book — close the job out.
            job.Status = CoverIngestionJobStatus.Exhausted;
            job.CompletedAt = now;
            job.LastError = "Book no longer exists.";
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(book.CoverUrl))
        {
            // A cover arrived by another path (manual upload / re-import enrich).
            job.Status = CoverIngestionJobStatus.Succeeded;
            job.CompletedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        // The provider lookup is the transient signal: if it throws, the provider
        // is unreachable → reschedule without consuming the give-up budget.
        var provider = sp.GetRequiredService<IBookDataProvider>();
        ExternalBookData? external;
        try
        {
            external = await provider.GetByIsbnAsync(job.Isbn, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RescheduleTransient(job, ex, now);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var resolver = sp.GetRequiredService<IBookCoverUrlResolver>();
        var acquisition = sp.GetRequiredService<IBookCoverAcquisition>();
        var candidates = new List<string?> { external?.CoverUrl, resolver.ResolveByIsbn(job.Isbn) };
        var blobUrl = await acquisition.AcquireAsync(job.Isbn, candidates, cancellationToken);

        if (blobUrl is not null)
        {
            book.UpdateDetails(coverUrl: blobUrl);
            book.RaiseUpdatedEvent(); // republish so Library/Social snapshots get the blob URL
            await bookRepository.UpdateAsync(book, cancellationToken);

            // Backfill the work's default cover if it still lacks one.
            var workRepository = sp.GetRequiredService<IWorkRepository>();
            var work = await workRepository.GetByIdAsync(book.WorkId, cancellationToken);
            if (work is not null && string.IsNullOrWhiteSpace(work.DefaultCoverUrl))
            {
                work.EnsureDefaultCover(blobUrl);
                await workRepository.UpdateAsync(work, cancellationToken);
            }

            job.Status = CoverIngestionJobStatus.Succeeded;
            job.CompletedAt = now;
            job.LastError = null;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cover ingestion job {JobId} found a cover for book {BookId}", job.Id, job.BookId);
            return;
        }

        // Confirmed no-cover: the provider responded but no real image exists.
        job.NoCoverAttempts++;
        if (CoverRetryPolicy.IsExhausted(job.NoCoverAttempts))
        {
            job.Status = CoverIngestionJobStatus.Exhausted;
            job.CompletedAt = now;
        }
        else
        {
            job.Status = CoverIngestionJobStatus.Pending;
            job.NextRetryAt = now.Add(CoverRetryPolicy.NextDelay(job.NoCoverAttempts));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<CoverIngestionJobEntity?> ClaimNextJobAsync(
        CatalogDbContext context,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;
            var staleBefore = now.Subtract(StaleProcessingAfter);

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var jobs = await context.CoverIngestionJobs
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM cover_ingestion_jobs
                    WHERE (status = 'Pending' AND next_retry_at <= {now})
                       OR (status = 'Processing' AND updated_at < {staleBefore})
                    ORDER BY next_retry_at
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                    """)
                .ToListAsync(cancellationToken);
            var job = jobs.FirstOrDefault();

            if (job is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            job.Status = CoverIngestionJobStatus.Processing;
            job.UpdatedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return job;
        });
    }

    private static void RescheduleTransient(CoverIngestionJobEntity job, Exception ex, DateTime now)
    {
        job.TransientFailures++;
        job.Status = CoverIngestionJobStatus.Pending;
        job.NextRetryAt = now.Add(CoverRetryPolicy.TransientBackoff);
        job.LastError = ex.Message;
    }

    private async Task RescheduleTransientAsync(Guid jobId, Exception exception, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var job = await context.CoverIngestionJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job is null)
            return;

        RescheduleTransient(job, exception, DateTime.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }
}
