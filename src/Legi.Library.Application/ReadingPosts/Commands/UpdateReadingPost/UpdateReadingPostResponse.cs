namespace Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;

public record UpdateReadingPostResponse(
    Guid PostId,
    string? Content,
    int? ProgressValue,
    string? ProgressType,
    DateTime UpdatedAt
);