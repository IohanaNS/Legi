using Legi.Catalog.Application.Books.DTOs;

namespace Legi.Catalog.Application.Books.Queries.GetBookDetails;

public record GetBookDetailsResponse(
    Guid Id,
    string Isbn,
    string Title,
    List<AuthorDto> Authors,
    string? Synopsis,
    int? PageCount,
    string? Publisher,
    string? CoverUrl,
    decimal AverageRating,
    int RatingsCount,
    int ReviewsCount,
    List<TagDto> Tags,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);