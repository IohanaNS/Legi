namespace Legi.Catalog.Infrastructure.Persistence.Entities;

public enum ExternalBookSearchJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public class ExternalBookSearchJobEntity
{
    public Guid Id { get; set; }
    public string QueryHash { get; set; } = null!;
    public string Query { get; set; } = null!;
    public ExternalBookSearchJobStatus Status { get; set; }
    public Guid RequestedByUserId { get; set; }
    public int MaxResults { get; set; }
    public int Attempts { get; set; }
    public DateTime NextRetryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CandidatesFound { get; set; }
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? Error { get; set; }
}
