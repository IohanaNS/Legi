using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Comments.EventHandlers;

/// <summary>
/// Creates a notification for the content owner when someone comments on their
/// post, review, or list. Runs alongside <see cref="CommentCreatedDomainEventHandler"/>
/// (the integration translator).
///
/// Self-suppression: if the commenter is the content owner, no notification is created.
///
/// Transaction: stages the notification (no SaveChanges) so it commits atomically
/// with the Comment row — domain events dispatch before the commit.
/// </summary>
public class CommentCreatedNotificationHandler(
    IContentSnapshotRepository contentSnapshotRepository,
    IUserProfileRepository userProfileRepository,
    INotificationRepository notificationRepository,
    ILogger<CommentCreatedNotificationHandler> logger)
    : INotificationHandler<CommentCreatedDomainEvent>
{
    public async Task Handle(
        CommentCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var snapshot = await contentSnapshotRepository.GetByTargetAsync(
            notification.TargetType, notification.TargetId, cancellationToken);
        if (snapshot is null)
        {
            logger.LogDebug(
                "No ContentSnapshot for ({TargetType} {TargetId}); skipping comment notification",
                notification.TargetType, notification.TargetId);
            return;
        }

        // Don't notify yourself for commenting on your own content.
        if (snapshot.OwnerId == notification.UserId)
            return;

        var actor = await userProfileRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (actor is null)
        {
            logger.LogDebug(
                "No UserProfile for actor {ActorId}; skipping comment notification", notification.UserId);
            return;
        }

        var entity = Notification.CreateComment(
            recipientId: snapshot.OwnerId,
            actorId: actor.UserId,
            actorUsername: actor.Username,
            actorAvatarUrl: actor.AvatarUrl,
            targetType: notification.TargetType,
            targetId: notification.TargetId,
            commentPreview: notification.Content);

        notificationRepository.StageAdd(entity);
    }
}
