using Legi.Contracts.Library;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Lists.IntegrationEventHandlers;

/// <summary>
/// Purges all Social state for a deleted list: its <c>ContentSnapshot</c>, plus
/// any likes, comments, and follows. All deletions are staged on the change
/// tracker; the dispatcher owns the commit so they land atomically with the inbox
/// row (decision 8.1). MUST NOT call SaveChangesAsync.
/// </summary>
public sealed class UserListDeletedIntegrationEventHandler(
    IContentSnapshotRepository contentSnapshotRepository,
    ILikeRepository likeRepository,
    ICommentRepository commentRepository,
    IListFollowRepository listFollowRepository,
    ILogger<UserListDeletedIntegrationEventHandler> logger)
    : INotificationHandler<UserListDeletedIntegrationEvent>
{
    public async Task Handle(
        UserListDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var listId = integrationEvent.ListId;

        await contentSnapshotRepository.StageDeleteByTargetAsync(
            InteractableType.List, listId, cancellationToken);
        await likeRepository.StageDeleteByTargetAsync(
            InteractableType.List, listId, cancellationToken);
        await commentRepository.StageDeleteByTargetAsync(
            InteractableType.List, listId, cancellationToken);
        await listFollowRepository.StageDeleteByListAsync(listId, cancellationToken);

        logger.LogInformation(
            "Staged purge of Social state (snapshot, likes, comments, follows) for deleted list {ListId}.",
            listId);
    }
}
