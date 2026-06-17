namespace Legi.Catalog.Domain.Repositories;

/// <summary>
/// Read-only repository for book queries and searches.
/// Separate from IBookRepository (write) following CQRS pattern.
/// </summary>
public interface IBookReadRepository
{
    Task<(List<BookSearchResult> Books, int TotalCount)> SearchAsync(
        string? searchTerm,
        string? authorSlug,
        IReadOnlyList<string>? tagSlugs,
        decimal? minRating,
        int pageNumber,
        int pageSize,
        BookSortBy sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    Task<BookDetailsResult?> GetBookDetailsByIdAsync(
        Guid bookId,
        CancellationToken cancellationToken = default);

    Task<BookDetailsResult?> GetBookDetailsByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the editions belonging to a work (the book + its siblings sharing
    /// the same <c>work_id</c>), so a book detail page can list "other editions".
    /// Ordered oldest-first.
    /// </summary>
    Task<List<EditionResult>> GetEditionsByWorkIdAsync(
        Guid workId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight summary of an edition (a <c>Book</c>) for listing the editions of a
/// work. Edition-specific fields only — work-level data lives elsewhere.
/// </summary>
public record EditionResult(
    Guid Id,
    string Isbn,
    string Title,
    string? CoverUrl,
    string? Publisher,
    int? PageCount
);