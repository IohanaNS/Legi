using Legi.Contracts.Social;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;

/// <summary>
/// Library's consumer for <see cref="CommentDeletedIntegrationEvent"/>. Decrements
/// <c>CommentsCount</c> (floored at zero) on the targeted <c>ReadingProgress</c>.
///
/// IDEMPOTENCY (MESSAGING-ARCHITECTURE-decisions.md §8.1.1): the inbox is the only
/// guard against double-counting — Library holds no Comment rows to reconcile
/// against. MUST NOT call SaveChangesAsync and MUST NOT use ExecuteUpdateAsync;
/// both commit before the inbox row and reopen the double-count window under
/// redelivery. The IntegrationEventDispatcher performs the single SaveChanges
/// that commits the counter change and the inbox row atomically.
/// </summary>
public sealed class CommentDeletedIntegrationEventHandler(
    IReadingPostRepository readingPostRepository,
    ILogger<CommentDeletedIntegrationEventHandler> logger)
    : INotificationHandler<CommentDeletedIntegrationEvent>
{
    public async Task Handle(
        CommentDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var post = await InteractionTargetResolver.ResolveTrackedPostAsync(
            readingPostRepository,
            integrationEvent.TargetType,
            integrationEvent.TargetId,
            logger,
            cancellationToken);

        if (post is null)
            return;

        post.DecrementComments();

        logger.LogDebug(
            "Decremented CommentsCount on post {PostId} (now {CommentsCount})",
            post.Id, post.CommentsCount);
    }
}
