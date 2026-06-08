using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Legi.Catalog.Infrastructure.ExternalServices;

public partial class ExternalBookSearchQueue(
    CatalogDbContext context,
    ILogger<ExternalBookSearchQueue> logger)
    : IExternalBookSearchQueue
{
    private const int RefreshAfterSeconds = 5;
    private static readonly TimeSpan RecentCompletedWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RecentEmptyCompletedWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RecentFailedWindow = TimeSpan.FromMinutes(10);

    public async Task<ExternalBookSearchEnrichment> QueueAsync(
        ExternalBookSearchQueueRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(request.SearchTerm);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return ExternalBookSearchEnrichment.NotApplicable();
        }

        var now = DateTime.UtcNow;
        var queryHash = ComputeSha256Hex(normalizedQuery);

        var activeJobExists = await context.ExternalBookSearchJobs
            .AsNoTracking()
            .AnyAsync(
                j => j.QueryHash == queryHash
                     && (j.Status == ExternalBookSearchJobStatus.Pending
                         || j.Status == ExternalBookSearchJobStatus.Processing),
                cancellationToken);

        if (activeJobExists)
        {
            return new ExternalBookSearchEnrichment(
                ExternalBookSearchEnrichmentStatuses.AlreadyQueued,
                "External search is already queued.",
                RefreshAfterSeconds);
        }

        var recentCompleted = await context.ExternalBookSearchJobs
            .AsNoTracking()
            .AnyAsync(
                j => j.QueryHash == queryHash
                     && j.Status == ExternalBookSearchJobStatus.Completed
                     && j.CompletedAt >= now.Subtract(RecentCompletedWindow)
                     && (j.CandidatesFound > 0 || j.ImportedCount > 0 || j.UpdatedCount > 0),
                cancellationToken);

        if (recentCompleted)
        {
            return new ExternalBookSearchEnrichment(
                ExternalBookSearchEnrichmentStatuses.RecentlyCompleted,
                "External search completed recently.");
        }

        var recentEmptyCompleted = await context.ExternalBookSearchJobs
            .AsNoTracking()
            .AnyAsync(
                j => j.QueryHash == queryHash
                     && j.Status == ExternalBookSearchJobStatus.Completed
                     && j.CompletedAt >= now.Subtract(RecentEmptyCompletedWindow),
                cancellationToken);

        if (recentEmptyCompleted)
        {
            return new ExternalBookSearchEnrichment(
                ExternalBookSearchEnrichmentStatuses.RecentlyCompleted,
                "External search completed recently.");
        }

        var recentFailed = await context.ExternalBookSearchJobs
            .AsNoTracking()
            .AnyAsync(
                j => j.QueryHash == queryHash
                     && j.Status == ExternalBookSearchJobStatus.Failed
                     && j.CompletedAt >= now.Subtract(RecentFailedWindow),
                cancellationToken);

        if (recentFailed)
        {
            return new ExternalBookSearchEnrichment(
                ExternalBookSearchEnrichmentStatuses.FailedRecently,
                "External search failed recently.");
        }

        context.ExternalBookSearchJobs.Add(new ExternalBookSearchJobEntity
        {
            Id = Guid.NewGuid(),
            QueryHash = queryHash,
            Query = normalizedQuery,
            Status = ExternalBookSearchJobStatus.Pending,
            RequestedByUserId = request.RequestedByUserId,
            MaxResults = request.MaxResults,
            Attempts = 0,
            NextRetryAt = now,
            CreatedAt = now
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogDebug(
                ex,
                "External book search job for query hash {QueryHash} was queued concurrently",
                queryHash);

            foreach (var entry in context.ChangeTracker.Entries<ExternalBookSearchJobEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.State = EntityState.Detached;
            }

            return new ExternalBookSearchEnrichment(
                ExternalBookSearchEnrichmentStatuses.AlreadyQueued,
                "External search is already queued.",
                RefreshAfterSeconds);
        }

        return new ExternalBookSearchEnrichment(
            ExternalBookSearchEnrichmentStatuses.Queued,
            "External search was queued.",
            RefreshAfterSeconds);
    }

    internal static string NormalizeQuery(string query)
    {
        var trimmed = query.Trim().ToLowerInvariant();
        return WhitespaceRegex().Replace(trimmed, " ");
    }

    private static string ComputeSha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
