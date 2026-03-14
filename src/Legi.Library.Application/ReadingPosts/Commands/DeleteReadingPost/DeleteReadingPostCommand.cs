using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.DeleteReadingPost;

public record DeleteReadingPostCommand(
    Guid PostId,
    Guid UserId
) : IRequest<Unit>;