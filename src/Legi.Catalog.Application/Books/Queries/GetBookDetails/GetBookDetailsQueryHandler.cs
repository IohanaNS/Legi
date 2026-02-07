using Legi.Catalog.Application.Books.DTOs;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Common.Mediator;
using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Books.Queries.GetBookDetails;

public class GetBookDetailsQueryHandler(IBookReadRepository bookReadRepository)
    : IRequestHandler<GetBookDetailsQuery, GetBookDetailsResponse>
{
    public async Task<GetBookDetailsResponse> Handle(
        GetBookDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var book = await bookReadRepository.GetBookDetailsByIdAsync(
            request.BookId,
            cancellationToken
        );

        if (book == null)
            throw new NotFoundException("Book", request.BookId);

        return new GetBookDetailsResponse(
            book.Id,
            book.Isbn,
            book.Title,
            book.Authors.Select(a => new AuthorDto(a.Name, a.Slug)).ToList(),
            book.Synopsis,
            book.PageCount,
            book.Publisher,
            book.CoverUrl,
            book.AverageRating,
            book.RatingsCount,
            book.ReviewsCount,
            book.Tags.Select(t => new TagDto(t.Name, t.Slug)).ToList(),
            book.CreatedByUserId,
            book.CreatedAt,
            book.UpdatedAt
        );
    }
}