using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Commands.UnfollowList;

public class UnfollowListCommandHandler(IListFollowRepository listFollowRepository)
    : IRequestHandler<UnfollowListCommand, Unit>
{
    public async Task<Unit> Handle(
        UnfollowListCommand request,
        CancellationToken cancellationToken)
    {
        var follow = await listFollowRepository.GetByUserAndListAsync(
            request.UserId, request.ListId, cancellationToken);

        // Idempotent: unfollowing a list you don't follow is a no-op.
        if (follow is not null)
            await listFollowRepository.DeleteAsync(follow, cancellationToken);

        return Unit.Value;
    }
}
