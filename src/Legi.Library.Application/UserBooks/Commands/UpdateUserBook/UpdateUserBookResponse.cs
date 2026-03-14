namespace Legi.Library.Application.UserBooks.Commands.UpdateUserBook;

public record UpdateUserBookResponse(
    Guid UserBookId,
    Guid BookId,
    string Status,
    int? ProgressValue,
    string? ProgressType,
    bool Wishlist,
    decimal? RatingStars,
    DateTime UpdatedAt
);