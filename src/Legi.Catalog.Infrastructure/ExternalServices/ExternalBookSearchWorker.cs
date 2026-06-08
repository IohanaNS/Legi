using Legi.Catalog.Application.Books.Commands.ProcessExternalBookSearchJob;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Legi.SharedKernel.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.ExternalServices;

public class ExternalBookSearchWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ExternalBookSearchWorker> logger)
    : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("External book search worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedJob = await ProcessNextJobAsync(stoppingToken);

                if (!processedJob)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "External book search worker poll failed");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var job = await ClaimNextJobAsync(context, cancellationToken);
        if (job is null)
        {
            return false;
        }

        try
        {
            var result = await mediator.Send(
                new ProcessExternalBookSearchJobCommand(
                    job.Query,
                    job.RequestedByUserId,
                    job.MaxResults),
                cancellationToken);

            job.Status = ExternalBookSearchJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.CandidatesFound = result.CandidatesFound;
            job.ImportedCount = result.ImportedCount;
            job.UpdatedCount = result.UpdatedCount;
            job.SkippedCount = result.SkippedCount;
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "External book search job {JobId} completed: {CandidatesFound} candidates, {ImportedCount} imported, {UpdatedCount} updated, {SkippedCount} skipped",
                job.Id,
                result.CandidatesFound,
                result.ImportedCount,
                result.UpdatedCount,
                result.SkippedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "External book search job {JobId} failed", job.Id);
            await MarkFailedOrRetryAsync(job.Id, ex, cancellationToken);
        }

        return true;
    }

    private static async Task<ExternalBookSearchJobEntity?> ClaimNextJobAsync(
        CatalogDbContext context,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var jobs = await context.ExternalBookSearchJobs
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM external_book_search_jobs
                    WHERE status = 'Pending' AND next_retry_at <= {now}
                    ORDER BY created_at
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

            job.Status = ExternalBookSearchJobStatus.Processing;
            job.Attempts++;
            job.StartedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return job;
        });
    }

    private async Task MarkFailedOrRetryAsync(
        Guid jobId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var job = await context.ExternalBookSearchJobs.FirstOrDefaultAsync(
            j => j.Id == jobId,
            cancellationToken);

        if (job is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        job.Error = exception.Message;

        if (job.Attempts >= MaxAttempts)
        {
            job.Status = ExternalBookSearchJobStatus.Failed;
            job.CompletedAt = now;
        }
        else
        {
            job.Status = ExternalBookSearchJobStatus.Pending;
            job.NextRetryAt = now.Add(GetBackoff(job.Attempts));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static TimeSpan GetBackoff(int attempts)
    {
        return attempts switch
        {
            <= 1 => TimeSpan.FromSeconds(30),
            2 => TimeSpan.FromMinutes(2),
            _ => TimeSpan.FromMinutes(5)
        };
    }
}
