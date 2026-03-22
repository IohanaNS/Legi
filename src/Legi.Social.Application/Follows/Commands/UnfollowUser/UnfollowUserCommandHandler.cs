using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Follows.Commands.UnfollowUser;

public class UnfollowUserCommandHandler(IFollowRepository followRepository)
    : IRequestHandler<UnfollowUserCommand>
{
    public async Task Handle(
        UnfollowUserCommand request,
        CancellationToken cancellationToken)
    {
        var follow = await followRepository.GetByPairAsync(
            request.FollowerId, request.FollowingId);

        if (follow is null)
            throw new NotFoundException(nameof(Follow), 
                $"({request.FollowerId}, {request.FollowingId})");

        // Raises FollowRemovedDomainEvent for counter decrement
        follow.MarkForRemoval();

        await followRepository.DeleteAsync(follow);
    }
}