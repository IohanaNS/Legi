using Legi.Contracts.Social;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Comments.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="CommentDeletedDomainEvent"/> into the
/// cross-context <see cref="CommentDeletedIntegrationEvent"/> and publishes it via
/// <see cref="IEventBus"/>. Library consumes it (4E) to decrement CommentsCount.
///
/// No SaveChangesAsync — <c>OutboxEventBus</c> writes the outbox row into the
/// current SocialDbContext, and the dispatcher commits it atomically with the
/// Comment removal (see MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 / 3.4).
/// </summary>
public class CommentDeletedDomainEventHandler(
    IEventBus eventBus,
    ILogger<CommentDeletedDomainEventHandler> logger)
    : INotificationHandler<CommentDeletedDomainEvent>
{
    public async Task Handle(
        CommentDeletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new CommentDeletedIntegrationEvent(
            TargetType: notification.TargetType.ToString(),
            TargetId: notification.TargetId,
            CommentId: notification.Id,
            UserId: notification.UserId);

        await eventBus.PublishAsync(integrationEvent, cancellationToken);

        logger.LogDebug(
            "Translated CommentDeletedDomainEvent (comment {CommentId} on {TargetType} {TargetId}) to integration event",
            notification.Id, notification.TargetType, notification.TargetId);
    }
}
