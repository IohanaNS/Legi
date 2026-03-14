namespace Legi.Library.Application.Common.DTOs;

public record UserListBookDto(
    Guid UserBookId,
    int Order,
    BookSnapshotDto Book,
    string Status,
    decimal? RatingStars,
    DateTime AddedAt
);