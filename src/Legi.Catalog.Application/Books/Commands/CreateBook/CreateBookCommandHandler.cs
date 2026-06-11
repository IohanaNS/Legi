using Legi.Catalog.Application.Books;
using Legi.Catalog.Application.Books.DTOs;
using Legi.SharedKernel.Mediator;
using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookCommandHandler(BookImportService bookImportService)
    : IRequestHandler<CreateBookCommand, CreateBookResponse>
{
    public async Task<CreateBookResponse> Handle(
        CreateBookCommand request,
        CancellationToken cancellationToken)
    {
        var book = await bookImportService.CreateManualAsync(
            new BookImportInput(
                request.Isbn,
                request.Title,
                request.Authors,
                request.CreatedByUserId,
                request.Synopsis,
                request.PageCount,
                request.Publisher,
                request.CoverUrl,
                request.Tags),
            cancellationToken);

        return ToResponse(book);
    }

    private static CreateBookResponse ToResponse(Book book)
    {
        return new CreateBookResponse(
            book.Id,
            book.Isbn.Value,
            book.Title,
            book.Authors.Select(a => new AuthorDto(a.Name, a.Slug)).ToList(),
            book.Synopsis,
            book.PageCount,
            book.Publisher,
            book.CoverUrl,
            book.AverageRating,
            book.RatingsCount,
            book.Tags.Select(t => new TagDto(t.Name, t.Slug)).ToList(),
            book.CreatedByUserId,
            book.CreatedAt
        );
    }
}
