using Legi.Contracts.Social;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Comments.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="CommentCreatedDomainEvent"/> into the
/// cross-context <see cref="ContentCommentedIntegrationEvent"/> (named for the
/// action on the target content, not the comment) and publishes it via
/// <see cref="IEventBus"/>. Library consumes it (4E) to increment CommentsCount.
///
/// No SaveChangesAsync — <c>OutboxEventBus</c> writes the outbox row into the
/// current SocialDbContext, and the dispatcher commits it atomically with the
/// Comment aggregate (see MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 / 3.4).
/// </summary>
public class CommentCreatedDomainEventHandler(
    IEventBus eventBus,
    ILogger<CommentCreatedDomainEventHandler> logger)
    : INotificationHandler<CommentCreatedDomainEvent>
{
    public async Task Handle(
        CommentCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ContentCommentedIntegrationEvent(
            TargetType: notification.TargetType.ToString(),
            TargetId: notification.TargetId,
            CommentId: notification.CommentId,
            UserId: notification.UserId);

        await eventBus.PublishAsync(integrationEvent, cancellationToken);

        logger.LogDebug(
            "Translated CommentCreatedDomainEvent (comment {CommentId} on {TargetType} {TargetId}) to integration event",
            notification.CommentId, notification.TargetType, notification.TargetId);
    }
}
