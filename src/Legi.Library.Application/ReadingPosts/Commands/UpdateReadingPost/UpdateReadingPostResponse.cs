namespace Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;

public record UpdateReadingPostResponse(
    Guid PostId,
    string? Content,
    bool IsSpoiler,
    int? ProgressValue,
    string? ProgressType,
    DateTime UpdatedAt
);
