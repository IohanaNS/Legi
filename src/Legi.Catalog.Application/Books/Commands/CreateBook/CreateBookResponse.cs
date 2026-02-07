using Legi.Catalog.Application.Books.DTOs;

namespace Legi.Catalog.Application.Books.Commands.CreateBook;

public record CreateBookResponse(
    Guid BookId,
    string Isbn,
    string Title,
    List<AuthorDto> Authors,
    string? Synopsis,
    int? PageCount,
    string? Publisher,
    string? CoverUrl,
    decimal AverageRating,
    int RatingsCount,
    List<TagDto> Tags,
    Guid CreatedByUserId,
    DateTime CreatedAt
);