using Legi.Catalog.Application.Books.Queries.SearchBooks;
using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Tests.Factories;

public static class SearchBooksQueryFactory
{
    public static SearchBooksQuery Create(
        Guid? authenticatedUserId = null,
        string? searchTerm = "clean",
        string? authorSlug = null,
        IReadOnlyList<string>? tagSlugs = null,
        decimal? minRating = 4.0m,
        int pageNumber = 1,
        int pageSize = 20,
        BookSortBy sortBy = BookSortBy.Relevance,
        bool sortDescending = true)
    {
        return new SearchBooksQuery(
            authenticatedUserId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            searchTerm,
            authorSlug,
            tagSlugs,
            minRating,
            pageNumber,
            pageSize,
            sortBy,
            sortDescending
        );
    }
}
