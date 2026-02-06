namespace Legi.Catalog.Domain.Repositories;

/// <summary>
/// Read-only repository for tag search and autocomplete.
/// Tags are Value Objects managed through the Book aggregate, 
/// but we need search capabilities for the UI.
/// </summary>
public interface ITagReadRepository
{
    /// <summary>
    /// Search tags by name prefix for autocomplete.
    /// </summary>
    Task<IReadOnlyList<TagSearchResult>> SearchAsync(
        string searchTerm, 
        int limit = 10, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most popular tags.
    /// </summary>
    Task<IReadOnlyList<TagSearchResult>> GetPopularAsync(
        int limit = 20, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for tag search results. Not a domain entity.
/// </summary>
public record TagSearchResult(string Name, string Slug, int UsageCount);