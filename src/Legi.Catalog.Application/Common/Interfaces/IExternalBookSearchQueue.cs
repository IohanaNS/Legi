namespace Legi.Catalog.Application.Common.Interfaces;

public static class ExternalBookSearchEnrichmentStatuses
{
    public const string NotApplicable = "NotApplicable";
    public const string NotNeeded = "NotNeeded";
    public const string Queued = "Queued";
    public const string AlreadyQueued = "AlreadyQueued";
    public const string RecentlyCompleted = "RecentlyCompleted";
    public const string FailedRecently = "FailedRecently";
}

public record ExternalBookSearchEnrichment(
    string Status,
    string? Message = null,
    int? RefreshAfterSeconds = null)
{
    public static ExternalBookSearchEnrichment NotApplicable(string? message = null) =>
        new(ExternalBookSearchEnrichmentStatuses.NotApplicable, message);

    public static ExternalBookSearchEnrichment NotNeeded(string? message = null) =>
        new(ExternalBookSearchEnrichmentStatuses.NotNeeded, message);
}

public interface IExternalBookSearchQueue
{
    Task<ExternalBookSearchEnrichment> QueueAsync(
        ExternalBookSearchQueueRequest request,
        CancellationToken cancellationToken = default);
}

public record ExternalBookSearchQueueRequest(
    string SearchTerm,
    Guid RequestedByUserId,
    int MaxResults);
