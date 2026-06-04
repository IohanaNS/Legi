using Legi.Contracts.Social;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;

/// <summary>
/// Library's consumer for <see cref="ContentUnlikedIntegrationEvent"/>. Decrements
/// <c>LikesCount</c> (floored at zero) on the targeted <c>ReadingProgress</c>.
///
/// IDEMPOTENCY (MESSAGING-ARCHITECTURE-decisions.md §8.1.1): the inbox is the only
/// guard against double-counting — Library holds no Like rows to reconcile against.
/// MUST NOT call SaveChangesAsync and MUST NOT use ExecuteUpdateAsync; both commit
/// before the inbox row and reopen the double-count window under redelivery. The
/// IntegrationEventDispatcher performs the single SaveChanges that commits the
/// counter change and the inbox row atomically.
/// </summary>
public sealed class ContentUnlikedIntegrationEventHandler(
    IReadingPostRepository readingPostRepository,
    ILogger<ContentUnlikedIntegrationEventHandler> logger)
    : INotificationHandler<ContentUnlikedIntegrationEvent>
{
    public async Task Handle(
        ContentUnlikedIntegrationEvent integrationEvent,
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

        post.DecrementLikes();

        logger.LogDebug(
            "Decremented LikesCount on post {PostId} (now {LikesCount})",
            post.Id, post.LikesCount);
    }
}
