namespace Legi.Library.Application.ReadingPosts.Commands.CreateBookReview;

public record CreateBookReviewResponse(
    Guid ReviewId,
    Guid UserBookId,
    string Content,
    bool IsSpoiler,
    decimal Stars,
    DateTime CreatedAt
);
