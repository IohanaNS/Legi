using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Commands.UnfollowList;

public record UnfollowListCommand(
    Guid UserId,
    Guid ListId) : IRequest<Unit>;
