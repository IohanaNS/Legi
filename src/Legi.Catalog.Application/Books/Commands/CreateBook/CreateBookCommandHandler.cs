using Legi.Catalog.Application.Books.DTOs;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.SharedKernel.Mediator;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;

namespace Legi.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookCommandHandler(IBookRepository bookRepository)
    : IRequestHandler<CreateBookCommand, CreateBookResponse>
{
    public async Task<CreateBookResponse> Handle(
        CreateBookCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate ISBN and check if book already exists
        var isbn = Isbn.Create(request.Isbn);
        
        var existingBook = await bookRepository.GetByIsbnAsync(isbn.Value, cancellationToken);
        if (existingBook != null)
            throw new ConflictException($"A book with ISBN '{request.Isbn}' already exists.");

        // 2. Create Author value objects
        var authors = request.Authors
            .Select(Author.Create)
            .ToList();

        // 3. Create Tag value objects (if provided)
        var tags = request.Tags?
            .Select(Tag.Create)
            .ToList();

        // 4. Create Book aggregate
        var book = Book.Create(
            isbn,
            request.Title,
            authors,
            request.CreatedByUserId,
            request.Synopsis,
            request.PageCount,
            request.Publisher,
            request.CoverUrl,
            tags
        );

        // 5. Persist
        await bookRepository.AddAsync(book, cancellationToken);

        // 6. Return response
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