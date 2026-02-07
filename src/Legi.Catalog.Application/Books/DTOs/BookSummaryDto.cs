namespace Legi.Catalog.Application.Books.DTOs;

/// <summary>
/// Lightweight DTO for book listings and search results
/// </summary>
public record BookSummaryDto(
    Guid Id,
    string Isbn,
    string Title,
    List<AuthorDto> Authors,
    string? CoverUrl,
    decimal AverageRating,
    int RatingsCount,
    List<TagDto> Tags
);