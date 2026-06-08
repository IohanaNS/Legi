namespace Legi.Catalog.Application.Common.Interfaces;

/// <summary>
/// Port for external book data enrichment.
/// Infrastructure implements this with one or more concrete API providers
/// using a fallback chain (Open Library → Google Books).
/// </summary>
public interface IBookDataProvider
{
    /// <summary>
    /// Attempts to fetch book metadata from external sources by ISBN.
    /// Returns null if no provider found data — this is NOT an error,
    /// as user-provided data may be sufficient.
    /// Individual provider failures are logged as warnings but never thrown.
    /// </summary>
    Task<ExternalBookData?> GetByIsbnAsync(string isbn, CancellationToken ct = default);

    /// <summary>
    /// Searches external sources by free text and returns normalized candidates.
    /// Provider failures are isolated and should not fail the search pipeline.
    /// </summary>
    Task<IReadOnlyList<ExternalBookCandidate>> SearchAsync(
        string searchTerm,
        int maxResults,
        CancellationToken ct = default);
}

/// <summary>
/// Normalized book data from any external provider.
/// All fields are nullable — external APIs frequently return partial data.
/// </summary>
public record ExternalBookData
{
    public string? Title { get; init; }
    public IReadOnlyList<string>? Authors { get; init; }
    public string? Synopsis { get; init; }
    public int? PageCount { get; init; }
    public string? Publisher { get; init; }
    public string? CoverUrl { get; init; }
    public string? Language { get; init; }

    /// <summary>
    /// Which provider returned this data (e.g. "OpenLibrary", "GoogleBooks").
    /// For logging/debugging only — never persisted.
    /// </summary>
    public string? ProviderName { get; init; }
}

/// <summary>
/// Normalized external search candidate from any provider.
/// Provider-specific identifiers and metadata are used only for dedupe/logging
/// and are not persisted to Catalog books.
/// </summary>
public record ExternalBookCandidate
{
    public string Provider { get; init; } = null!;
    public string? ProviderBookId { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public string? Title { get; init; }
    public IReadOnlyList<string> Authors { get; init; } = [];
    public string? Synopsis { get; init; }
    public int? PageCount { get; init; }
    public string? Publisher { get; init; }
    public string? CoverUrl { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Language { get; init; }
    public string? PublishedDate { get; init; }
}
