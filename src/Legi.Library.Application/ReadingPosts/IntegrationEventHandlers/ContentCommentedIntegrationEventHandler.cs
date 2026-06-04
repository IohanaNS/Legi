using Legi.Contracts.Social;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;

/// <summary>
/// Library's consumer for <see cref="ContentCommentedIntegrationEvent"/>.
/// Increments <c>CommentsCount</c> on the targeted <c>ReadingProgress</c>.
///
/// IDEMPOTENCY (MESSAGING-ARCHITECTURE-decisions.md §8.1.1): the inbox is the only
/// guard against double-counting — Library holds no Comment rows to reconcile
/// against. MUST NOT call SaveChangesAsync and MUST NOT use ExecuteUpdateAsync;
/// both commit before the inbox row and reopen the double-count window under
/// redelivery. The IntegrationEventDispatcher performs the single SaveChanges
/// that commits the counter change and the inbox row atomically.
/// </summary>
public sealed class ContentCommentedIntegrationEventHandler(
    IReadingPostRepository readingPostRepository,
    ILogger<ContentCommentedIntegrationEventHandler> logger)
    : INotificationHandler<ContentCommentedIntegrationEvent>
{
    public async Task Handle(
        ContentCommentedIntegrationEvent integrationEvent,
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

        post.IncrementComments();

        logger.LogDebug(
            "Incremented CommentsCount on post {PostId} (now {CommentsCount})",
            post.Id, post.CommentsCount);
    }
}
