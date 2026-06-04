using Legi.Contracts.Social;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Likes.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="ContentUnlikedDomainEvent"/> into the
/// cross-context <see cref="ContentUnlikedIntegrationEvent"/> and publishes it via
/// <see cref="IEventBus"/>. Library consumes it (4E) to decrement LikesCount.
///
/// No SaveChangesAsync — <c>OutboxEventBus</c> writes the outbox row into the
/// current SocialDbContext, and the dispatcher commits it atomically with the
/// Like aggregate (see MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 / 3.4).
/// </summary>
public class ContentUnlikedDomainEventHandler(
    IEventBus eventBus,
    ILogger<ContentUnlikedDomainEventHandler> logger)
    : INotificationHandler<ContentUnlikedDomainEvent>
{
    public async Task Handle(
        ContentUnlikedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ContentUnlikedIntegrationEvent(
            TargetType: notification.TargetType.ToString(),
            TargetId: notification.TargetId,
            UserId: notification.UserId);

        await eventBus.PublishAsync(integrationEvent, cancellationToken);

        logger.LogDebug(
            "Translated ContentUnlikedDomainEvent ({TargetType} {TargetId}) to integration event",
            notification.TargetType, notification.TargetId);
    }
}
