using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Comments.EventHandlers;

/// <summary>
/// Stub handler for CommentCreatedDomainEvent.
/// Consumer lives in Library (incrementing CommentsCount on ReadingPost/UserList).
/// 
/// ⚠️ TODO: When RabbitMQ is implemented, publish ContentCommentedIntegrationEvent here:
///   await messageBus.PublishAsync(new ContentCommentedIntegrationEvent(
///       notification.TargetType.ToString(), notification.TargetId, notification.CommentId, notification.UserId));
/// </summary>
public class CommentCreatedDomainEventHandler(
    ILogger<CommentCreatedDomainEventHandler> logger)
    : INotificationHandler<CommentCreatedDomainEvent>
{
    public Task Handle(
        CommentCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Comment created — CommentId: {CommentId}, TargetType: {TargetType}, TargetId: {TargetId}. " +
            "Integration event to Library pending RabbitMQ implementation.",
            notification.CommentId, notification.TargetType, notification.TargetId);

        return Task.CompletedTask;
    }
}