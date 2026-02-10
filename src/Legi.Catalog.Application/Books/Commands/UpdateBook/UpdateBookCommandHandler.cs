using Legi.Catalog.Application.Books.DTOs;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Books.Commands.UpdateBook;

public class UpdateBookCommandHandler(IBookRepository bookRepository)
    : IRequestHandler<UpdateBookCommand, UpdateBookResponse>
{
    public async Task<UpdateBookResponse> Handle(
        UpdateBookCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get existing book
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);

        if (book == null)
            throw new NotFoundException("Book", request.BookId);

        // 2. Update basic details if provided
        if (request.Title != null || request.Synopsis != null ||
            request.PageCount != null || request.Publisher != null || request.CoverUrl != null)
        {
            book.UpdateDetails(
                request.Title,
                request.Synopsis,
                request.PageCount,
                request.Publisher,
                request.CoverUrl
            );
        }

        // 3. Update authors if provided
        if (request.Authors != null && request.Authors.Count > 0)
        {
            var authors = request.Authors
                .Select(Author.Create)
                .ToList();

            book.SetAuthors(authors);
        }

        // 4. Update tags if provided
        if (request.Tags != null)
        {
            // Clear existing tags and add new ones
            book.ClearTags();

            if (request.Tags.Count > 0)
            {
                var tags = request.Tags
                    .Select(Tag.Create)
                    .ToList();

                book.AddTags(tags);
            }
        }

        // 5. Persist changes
        await bookRepository.UpdateAsync(book, cancellationToken);

        // 6. Return response
        return new UpdateBookResponse(
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
            book.UpdatedAt
        );
    }
}