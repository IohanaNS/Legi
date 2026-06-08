namespace Legi.Library.Application.Common.DTOs;

public record ReadingPostDto(
    Guid PostId,
    Guid UserBookId,
    string? Content,
    bool IsSpoiler,
    int? ProgressValue,
    string? ProgressType,
    DateOnly ReadingDate,
    int LikesCount,
    int CommentsCount,
    DateTime CreatedAt
);
