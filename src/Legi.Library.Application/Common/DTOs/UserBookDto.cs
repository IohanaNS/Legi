namespace Legi.Library.Application.Common.DTOs;

public record UserBookDto(
    Guid UserBookId,
    Guid BookId,
    string Status,
    int? ProgressValue,
    string? ProgressType,
    bool Wishlist,
    decimal? RatingStars,
    BookSnapshotDto Book,
    DateTime CreatedAt,
    DateTime UpdatedAt
);