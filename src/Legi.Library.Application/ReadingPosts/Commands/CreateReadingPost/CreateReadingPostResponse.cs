namespace Legi.Library.Application.ReadingPosts.Commands.CreateReadingPost;

public record CreateReadingPostResponse(
    Guid PostId,
    Guid UserBookId,
    string? Content,
    int? ProgressValue,
    string? ProgressType,
    DateOnly ReadingDate,
    DateTime CreatedAt
);