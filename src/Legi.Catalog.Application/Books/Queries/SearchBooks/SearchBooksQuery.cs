using Legi.Catalog.Application.Common.Mediator;
using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Books.Queries.SearchBooks;

public record SearchBooksQuery(
    string? SearchTerm = null,
    string? AuthorSlug = null,
    string? TagSlug = null,
    decimal? MinRating = null,
    int PageNumber = 1,
    int PageSize = 20,
    BookSortBy SortBy = BookSortBy.Relevance,
    bool SortDescending = true
) : IRequest<SearchBooksResponse>;