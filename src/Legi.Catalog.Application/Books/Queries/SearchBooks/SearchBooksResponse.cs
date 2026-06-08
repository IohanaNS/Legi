using Legi.Catalog.Application.Books.DTOs;
using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Application.Books.Queries.SearchBooks;

public record SearchBooksResponse(
    List<BookSummaryDto> Books,
    PaginationMetadata Pagination,
    ExternalBookSearchEnrichment Enrichment
);

public record PaginationMetadata(
    int CurrentPage,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext
);
