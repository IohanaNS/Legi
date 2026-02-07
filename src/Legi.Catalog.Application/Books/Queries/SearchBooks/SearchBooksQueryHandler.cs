using Legi.Catalog.Application.Books.DTOs;
using Legi.Catalog.Application.Common.Mediator;
using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Books.Queries.SearchBooks;

public class SearchBooksQueryHandler(IBookReadRepository bookReadRepository)
    : IRequestHandler<SearchBooksQuery, SearchBooksResponse>
{
    public async Task<SearchBooksResponse> Handle(
        SearchBooksQuery request,
        CancellationToken cancellationToken)
    {
        // Execute search
        var (books, totalCount) = await bookReadRepository.SearchAsync(
            request.SearchTerm,
            request.AuthorSlug,
            request.TagSlug,
            request.MinRating,
            request.PageNumber,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            cancellationToken
        );

        // Map to DTOs
        var bookDtos = books.Select(b => new BookSummaryDto(
            b.Id,
            b.Isbn,
            b.Title,
            b.Authors.Select(a => new AuthorDto(a.Name, a.Slug)).ToList(),
            b.CoverUrl,
            b.AverageRating,
            b.RatingsCount,
            b.Tags.Select(t => new TagDto(t.Name, t.Slug)).ToList()
        )).ToList();

        // Build pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var pagination = new PaginationMetadata(
            CurrentPage: request.PageNumber,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            HasPrevious: request.PageNumber > 1,
            HasNext: request.PageNumber < totalPages
        );

        return new SearchBooksResponse(bookDtos, pagination);
    }
}