using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Likes.EventHandlers;

/// <summary>
/// Creates a notification for the content owner when someone likes their post,
/// review, or list. Runs alongside <see cref="ContentLikedDomainEventHandler"/>
/// (the integration translator) — the mediator fans an event out to every
/// <see cref="INotificationHandler{T}"/>.
///
/// Self-suppression: if the liker is the content owner, no notification is created.
///
/// Transaction: domain events dispatch inside <c>SavingChangesAsync</c>, before the
/// commit, so this stages the notification (no SaveChanges) and it commits
/// atomically with the Like row.
/// </summary>
public class ContentLikedNotificationHandler(
    IContentSnapshotRepository contentSnapshotRepository,
    IUserProfileRepository userProfileRepository,
    INotificationRepository notificationRepository,
    ILogger<ContentLikedNotificationHandler> logger)
    : INotificationHandler<ContentLikedDomainEvent>
{
    public async Task Handle(
        ContentLikedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var snapshot = await contentSnapshotRepository.GetByTargetAsync(
            notification.TargetType, notification.TargetId, cancellationToken);
        if (snapshot is null)
        {
            logger.LogDebug(
                "No ContentSnapshot for ({TargetType} {TargetId}); skipping like notification",
                notification.TargetType, notification.TargetId);
            return;
        }

        // Don't notify yourself for liking your own content.
        if (snapshot.OwnerId == notification.UserId)
            return;

        var actor = await userProfileRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (actor is null)
        {
            logger.LogDebug(
                "No UserProfile for actor {ActorId}; skipping like notification", notification.UserId);
            return;
        }

        var entity = Notification.CreateLike(
            recipientId: snapshot.OwnerId,
            actorId: actor.UserId,
            actorUsername: actor.Username,
            actorAvatarUrl: actor.AvatarUrl,
            targetType: notification.TargetType,
            targetId: notification.TargetId);

        notificationRepository.StageAdd(entity);
    }
}
