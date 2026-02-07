using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Domain.Repositories;

/// <summary>
/// Sorting options for book search results.
/// </summary>
public enum BookSortBy
{
    Relevance,
    Title,
    AverageRating,
    RatingsCount,
    CreatedAt
}

/// <summary>
/// Write a repository for book persistence operations.
/// </summary>
public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    Task AddAsync(Book book, CancellationToken cancellationToken = default);
    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
}


/// <summary>
/// DTO for book search results (optimized for listings)
/// </summary>
public record BookSearchResult(
    Guid Id,
    string Isbn,
    string Title,
    List<(string Name, string Slug)> Authors,
    string? CoverUrl,
    decimal AverageRating,
    int RatingsCount,
    List<(string Name, string Slug)> Tags
);

/// <summary>
/// DTO for complete book details
/// </summary>
public record BookDetailsResult(
    Guid Id,
    string Isbn,
    string Title,
    List<(string Name, string Slug)> Authors,
    string? Synopsis,
    int? PageCount,
    string? Publisher,
    string? CoverUrl,
    decimal AverageRating,
    int RatingsCount,
    int ReviewsCount,
    List<(string Name, string Slug)> Tags,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);