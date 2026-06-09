using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;

/// <summary>
/// Shared guard logic for the four Social → Library counter consumers. Resolves
/// the <see cref="ReadingProgress"/> a like/comment event targets, applying the
/// two no-op rules both share. Returns the <b>tracked</b> entity ready to mutate,
/// or <c>null</c> when the handler must ack without changes.
/// </summary>
internal static class InteractionTargetResolver
{
    private const string PostTargetType = "Post";
    private const string ReviewTargetType = "Review";

    /// <summary>
    /// Returns the tracked post for the event, or <c>null</c> to no-op-and-ack:
    /// <list type="bullet">
    ///   <item><b>TargetType is neither "Post" nor "Review"</b> (List): non-interactable
    ///   in Library, cannot legitimately occur — log a warning and ack (do not throw).</item>
    ///   <item><b>Post not found</b>: a <i>terminal</i> no-op. A missing post was
    ///   deleted (permanent), not a propagation race like 4C's missing snapshot —
    ///   throwing here would redeliver forever on a deleted post's stray like.</item>
    /// </list>
    /// Reviews are stored as <see cref="ReadingProgress"/> rows too (the review's id
    /// is the post id), so both target types resolve through the same repository.
    /// </summary>
    public static async Task<ReadingProgress?> ResolveTrackedPostAsync(
        IReadingPostRepository repository,
        string targetType,
        Guid targetId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(targetType, PostTargetType, StringComparison.Ordinal)
            && !string.Equals(targetType, ReviewTargetType, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Ignoring interaction event for non-interactable TargetType {TargetType} (target {TargetId}); " +
                "only posts and reviews carry counters in Library.",
                targetType, targetId);
            return null;
        }

        // Tracked load (GetByIdAsync does not use AsNoTracking) so the mutation
        // the caller applies is picked up by the dispatcher's single SaveChanges.
        var post = await repository.GetByIdAsync(targetId, cancellationToken);
        if (post is null)
        {
            logger.LogWarning(
                "Reading post {PostId} not found for interaction event; treating as a terminal " +
                "no-op (post was deleted). Acking without requeue.",
                targetId);
        }

        return post;
    }
}
