using Legi.Catalog.Application.Books.Queries.SearchBooks;
using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Tests.Factories;

public static class SearchBooksQueryFactory
{
    public static SearchBooksQuery Create(
        string? searchTerm = "clean",
        string? authorSlug = null,
        string? tagSlug = null,
        decimal? minRating = 4.0m,
        int pageNumber = 1,
        int pageSize = 20,
        BookSortBy sortBy = BookSortBy.Relevance,
        bool sortDescending = true)
    {
        return new SearchBooksQuery(
            searchTerm,
            authorSlug,
            tagSlug,
            minRating,
            pageNumber,
            pageSize,
            sortBy,
            sortDescending
        );
    }
}
