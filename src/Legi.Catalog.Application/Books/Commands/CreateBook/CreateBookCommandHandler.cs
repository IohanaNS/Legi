using Legi.Catalog.Application.Books.DTOs;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookCommandHandler(
    IBookRepository bookRepository,
    IBookDataProvider bookDataProvider)
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

        // 2. Fetch external data for enrichment
        var externalData = await bookDataProvider.GetByIsbnAsync(isbn.Value, cancellationToken);

        // 3. Merge: user input takes priority, external API fills the gaps
        var title = UseUserValueOrFallback(request.Title, externalData?.Title);
        var authorNames = request.Authors?.Count > 0
            ? request.Authors
            : externalData?.Authors?.ToList();
        var synopsis = UseUserValueOrFallback(request.Synopsis, externalData?.Synopsis);
        var pageCount = request.PageCount ?? externalData?.PageCount;
        var publisher = UseUserValueOrFallback(request.Publisher, externalData?.Publisher);
        var coverUrl = UseUserValueOrFallback(request.CoverUrl, externalData?.CoverUrl);

        // 4. Validate mandatory fields AFTER merge
        //    Title and Authors are only required if neither the user nor the API provided them.
        //    This is validated here (not in FluentValidation) because it depends on the merge result.
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException(
                "Title is required when not available from external book sources.");

        if (authorNames is null || authorNames.Count == 0)
            throw new DomainException(
                "At least one author is required when not available from external book sources.");

        // 5. Create value objects
        var authors = authorNames.Select(Author.Create).ToList();
        var tags = request.Tags?.Select(Tag.Create).ToList();

        // 6. Create Book aggregate
        var book = Book.Create(
            isbn,
            title,
            authors,
            request.CreatedByUserId,
            synopsis,
            pageCount,
            publisher,
            coverUrl,
            tags
        );

        // 7. Persist
        await bookRepository.AddAsync(book, cancellationToken);

        // 8. Return response
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

    /// <summary>
    /// User-provided value takes priority. Falls back to external API value.
    /// Treats whitespace-only strings as empty (not provided).
    /// </summary>
    private static string? UseUserValueOrFallback(string? userValue, string? externalValue)
    {
        return !string.IsNullOrWhiteSpace(userValue) ? userValue : externalValue;
    }
}