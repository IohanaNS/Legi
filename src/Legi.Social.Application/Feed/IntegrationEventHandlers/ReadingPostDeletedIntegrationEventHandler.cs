using Legi.Contracts.Library;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Feed.IntegrationEventHandlers;

/// <summary>
/// Purges all Social-side traces of a deleted reading post: its ContentSnapshot,
/// its likes, its comments, and the FeedItem that referenced it.
///
/// No lookups (the purge is keyed entirely by PostId) and no re-emitted domain
/// events: deleting these likes/comments is a cascade from the upstream content
/// deletion, not a user un-like/un-comment, so raising ContentUnliked/CommentDeleted
/// here would be wrong and could fan out further work (decision 8.1.2). All four
/// deletes are idempotent — a redelivery (or a post that had no likes/comments)
/// simply removes nothing.
///
/// MUST NOT call SaveChangesAsync — the dispatcher commits the four staged deletes
/// atomically with the inbox row (decisions 8.1 / 8.1.3).
/// </summary>
public sealed class ReadingPostDeletedIntegrationEventHandler(
    IContentSnapshotRepository contentSnapshotRepository,
    ILikeRepository likeRepository,
    ICommentRepository commentRepository,
    IFeedItemRepository feedItemRepository,
    ILogger<ReadingPostDeletedIntegrationEventHandler> logger)
    : INotificationHandler<ReadingPostDeletedIntegrationEvent>
{
    public async Task Handle(
        ReadingPostDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var postId = integrationEvent.PostId;

        await contentSnapshotRepository.StageDeleteByTargetAsync(
            InteractableType.Post, postId, cancellationToken);
        await likeRepository.StageDeleteByTargetAsync(
            InteractableType.Post, postId, cancellationToken);
        await commentRepository.StageDeleteByTargetAsync(
            InteractableType.Post, postId, cancellationToken);
        await feedItemRepository.StageDeleteByReferenceAsync(postId, cancellationToken);

        logger.LogInformation(
            "Staged purge of ContentSnapshot/likes/comments/FeedItem for deleted post {PostId}",
            postId);
    }
}
