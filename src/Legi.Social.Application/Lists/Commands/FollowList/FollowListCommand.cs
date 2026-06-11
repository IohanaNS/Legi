using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Commands.FollowList;

public record FollowListCommand(
    Guid UserId,
    Guid ListId) : IRequest<Unit>;
