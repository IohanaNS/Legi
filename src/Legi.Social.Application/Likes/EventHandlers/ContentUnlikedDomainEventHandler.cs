using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Likes.EventHandlers;

/// <summary>
/// Stub handler for ContentUnlikedDomainEvent.
/// Consumer lives in Library (decrementing LikesCount on ReadingPost/UserList).
///
/// ⚠️ TODO: When RabbitMQ is implemented, publish ContentUnlikedIntegrationEvent here:
///   await messageBus.PublishAsync(new ContentUnlikedIntegrationEvent(
///       notification.TargetType.ToString(), notification.TargetId));
/// </summary>
public class ContentUnlikedDomainEventHandler(
    ILogger<ContentUnlikedDomainEventHandler> logger)
    : INotificationHandler<ContentUnlikedDomainEvent>
{
    public Task Handle(
        ContentUnlikedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Content unliked — TargetType: {TargetType}, TargetId: {TargetId}, UserId: {UserId}. " +
            "Integration event to Library pending RabbitMQ implementation.",
            notification.TargetType, notification.TargetId, notification.UserId);

        return Task.CompletedTask;
    }
}
