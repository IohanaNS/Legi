using Legi.Library.Domain.Enums;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;

public record UpdateReadingPostCommand(
    Guid PostId,
    Guid UserId,
    string? Content,
    int? ProgressValue,
    ProgressType? ProgressType
) : IRequest<UpdateReadingPostResponse>;