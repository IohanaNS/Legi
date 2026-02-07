using Legi.Catalog.Application.Books.DTOs;

namespace Legi.Catalog.Application.Books.Queries.SearchBooks;

public record SearchBooksResponse(
    List<BookSummaryDto> Books,
    PaginationMetadata Pagination
);

public record PaginationMetadata(
    int CurrentPage,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext
);