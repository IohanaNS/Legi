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
    Task<Book?> FindByTitleAndFirstAuthorAsync(
        string title,
        string firstAuthor,
        CancellationToken cancellationToken = default);
    Task AddAsync(Book book, CancellationToken cancellationToken = default);
    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
    Task DeleteAsync(Book book, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonymizes the creator field on all books created by the given user.
    /// Uses a bulk SQL update — does not call SaveChangesAsync.
    ///
    /// Called by <c>UserDeletedIntegrationEventHandler</c> in response to a user
    /// account deletion. Idempotent: running twice updates zero rows the second
    /// time.
    /// </summary>
    /// <returns>The number of books anonymized.</returns>
    Task<int> AnonymizeCreatorsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rating totals for a work's editions EXCLUDING one edition (the one currently
    /// being recomputed, whose new aggregate is read from its tracked entity). Used
    /// to compose the work-level rating as a weighted average across editions.
    /// </summary>
    Task<EditionRatingTotals> GetEditionRatingTotalsForWorkAsync(
        Guid workId, Guid excludeBookId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Summed ratings across a set of editions: total ratings and the sum of
/// <c>average_rating * ratings_count</c> (so a weighted average is
/// <c>WeightedRatingSum / RatingsCount</c>).
/// </summary>
public readonly record struct EditionRatingTotals(int RatingsCount, decimal WeightedRatingSum);


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
    Guid? CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid WorkId
);
