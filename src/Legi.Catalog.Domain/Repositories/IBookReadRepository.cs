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
        string? tagSlug,
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
}