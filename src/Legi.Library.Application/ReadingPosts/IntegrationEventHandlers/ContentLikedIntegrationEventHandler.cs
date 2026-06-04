using Legi.Contracts.Social;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;

/// <summary>
/// Library's consumer for <see cref="ContentLikedIntegrationEvent"/>. Increments
/// <c>LikesCount</c> on the targeted <c>ReadingProgress</c>.
///
/// IDEMPOTENCY (MESSAGING-ARCHITECTURE-decisions.md §8.1.1): an increment has no
/// natural-key defense here — Library holds no Like rows to check against (they
/// live in Social). The inbox is the ONLY guard against double-counting, so this
/// handler MUST NOT call SaveChangesAsync and MUST NOT use ExecuteUpdateAsync.
/// Both commit before the inbox row is written, reopening the double-count window
/// under redelivery. Instead it mutates the tracked entity and lets the
/// IntegrationEventDispatcher perform the single SaveChanges that commits the
/// counter change and the inbox row atomically.
/// </summary>
public sealed class ContentLikedIntegrationEventHandler(
    IReadingPostRepository readingPostRepository,
    ILogger<ContentLikedIntegrationEventHandler> logger)
    : INotificationHandler<ContentLikedIntegrationEvent>
{
    public async Task Handle(
        ContentLikedIntegrationEvent integrationEvent,
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

        post.IncrementLikes();

        logger.LogDebug(
            "Incremented LikesCount on post {PostId} (now {LikesCount})",
            post.Id, post.LikesCount);
    }
}
