namespace Legi.Catalog.Domain.Repositories;

/// <summary>
/// Read-only repository for author search and autocomplete.
/// Authors are Value Objects managed through the Book aggregate,
/// but we need search capabilities for the UI.
/// </summary>
public interface IAuthorReadRepository
{
    /// <summary>
    /// Search authors by name prefix for autocomplete.
    /// </summary>
    Task<IReadOnlyList<AuthorSearchResult>> SearchAsync(
        string searchTerm,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most prolific authors (by book count).
    /// </summary>
    Task<IReadOnlyList<AuthorSearchResult>> GetPopularAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get author by slug.
    /// </summary>
    Task<AuthorSearchResult?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for author search results. Not a domain entity.
/// </summary>
public record AuthorSearchResult(string Name, string Slug, int BooksCount);