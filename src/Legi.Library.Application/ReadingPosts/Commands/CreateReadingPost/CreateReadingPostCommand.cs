using Legi.Library.Domain.Enums;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.CreateReadingPost;

public record CreateReadingPostCommand(
    Guid UserBookId,
    Guid UserId,
    string? Content,
    int? ProgressValue,
    ProgressType? ProgressType,
    DateOnly? ReadingDate
) : IRequest<CreateReadingPostResponse>;