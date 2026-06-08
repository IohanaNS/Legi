namespace Legi.Catalog.Application.Books.Commands.ProcessExternalBookSearchJob;

public record ProcessExternalBookSearchJobResponse(
    int CandidatesFound,
    int ImportedCount,
    int UpdatedCount,
    int SkippedCount
);
