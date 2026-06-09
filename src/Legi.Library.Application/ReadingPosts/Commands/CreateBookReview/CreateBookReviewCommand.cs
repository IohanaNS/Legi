using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.CreateBookReview;

public record CreateBookReviewCommand(
    Guid UserBookId,
    Guid UserId,
    string Content,
    decimal Stars,
    bool IsSpoiler = false
) : IRequest<CreateBookReviewResponse>;
