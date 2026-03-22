using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Comments.EventHandlers;

/// <summary>
/// Stub handler for CommentDeletedDomainEvent.
/// Consumer lives in Library (decrementing CommentsCount on ReadingPost/UserList).
/// 
/// ⚠️ TODO: When RabbitMQ is implemented, publish CommentDeletedIntegrationEvent here.
/// </summary>
public class CommentDeletedDomainEventHandler(
    ILogger<CommentDeletedDomainEventHandler> logger)
    : INotificationHandler<CommentDeletedDomainEvent>
{
    public Task Handle(
        CommentDeletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Comment deleted — CommentId: {CommentId}, TargetType: {TargetType}, TargetId: {TargetId}. " +
            "Integration event to Library pending RabbitMQ implementation.",
            notification.Id, notification.TargetType, notification.TargetId);

        return Task.CompletedTask;
    }
}