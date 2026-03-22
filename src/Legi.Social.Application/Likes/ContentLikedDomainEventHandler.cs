using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Likes.EventHandlers;

/// <summary>
/// Stub handler for ContentLikedDomainEvent.
/// Consumer lives in Library (incrementing LikesCount on ReadingPost/UserList).
/// 
/// ⚠️ TODO: When RabbitMQ is implemented, publish ContentLikedIntegrationEvent here:
///   await messageBus.PublishAsync(new ContentLikedIntegrationEvent(
///       notification.TargetType.ToString(), notification.TargetId, notification.UserId));
/// </summary>
public class ContentLikedDomainEventHandler(
    ILogger<ContentLikedDomainEventHandler> logger)
    : INotificationHandler<ContentLikedDomainEvent>
{
    public Task Handle(
        ContentLikedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Content liked — TargetType: {TargetType}, TargetId: {TargetId}, UserId: {UserId}. " +
            "Integration event to Library pending RabbitMQ implementation.",
            notification.TargetType, notification.TargetId, notification.UserId);

        return Task.CompletedTask;
    }
}