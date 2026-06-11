using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Commands.FollowList;

public class FollowListCommandHandler(
    IContentSnapshotRepository contentSnapshotRepository,
    IListFollowRepository listFollowRepository)
    : IRequestHandler<FollowListCommand, Unit>
{
    public async Task<Unit> Handle(
        FollowListCommand request,
        CancellationToken cancellationToken)
    {
        // A list is followable only while it is public — which is exactly when a
        // ContentSnapshot exists for it.
        var snapshot = await contentSnapshotRepository.GetByTargetAsync(
            InteractableType.List, request.ListId, cancellationToken);
        if (snapshot is null)
            throw new NotFoundException(nameof(ContentSnapshot), $"(List, {request.ListId})");

        if (snapshot.OwnerId == request.UserId)
            throw new ForbiddenException("You cannot follow your own list.");

        var alreadyFollowing = await listFollowRepository.ExistsAsync(
            request.UserId, request.ListId, cancellationToken);
        if (alreadyFollowing)
            throw new ConflictException(
                $"User {request.UserId} already follows list {request.ListId}.");

        var follow = ListFollow.Create(request.UserId, request.ListId);
        await listFollowRepository.AddAsync(follow, cancellationToken);

        return Unit.Value;
    }
}
