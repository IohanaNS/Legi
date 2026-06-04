using Legi.Contracts.Library;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Feed.IntegrationEventHandlers;

/// <summary>
/// Projects a Library reading-status change into the feed. Only a transition to
/// "Finished" is feed-worthy — it becomes a <see cref="ActivityType.BookFinished"/>
/// FeedItem keyed by the actor. Every other status (Reading, Paused, Abandoned, …)
/// no-ops; the dispatcher still commits the inbox row so the message is acked.
///
/// No ContentSnapshot: a book-finished activity is not interactable.
///
/// Resolves actor + book display data from local read models (decisions 8.3 /
/// 2.6.1); a missing lookup throws to redeliver. MUST NOT call SaveChangesAsync
/// (decision 8.1).
/// </summary>
public sealed class ReadingStatusChangedIntegrationEventHandler(
    IUserProfileRepository userProfileRepository,
    IBookSnapshotRepository bookSnapshotRepository,
    IFeedItemRepository feedItemRepository,
    ILogger<ReadingStatusChangedIntegrationEventHandler> logger)
    : INotificationHandler<ReadingStatusChangedIntegrationEvent>
{
    private const string FinishedStatus = "Finished";

    public async Task Handle(
        ReadingStatusChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(integrationEvent.NewStatus, FinishedStatus, StringComparison.Ordinal))
        {
            logger.LogDebug(
                "ReadingStatusChanged for user {UserId} book {BookId} to {Status} is not feed-worthy",
                integrationEvent.UserId, integrationEvent.BookId, integrationEvent.NewStatus);
            return;
        }

        var profile = await FeedLookups.GetProfileOrThrowAsync(
            userProfileRepository, integrationEvent.UserId, logger, cancellationToken);
        var book = await FeedLookups.GetBookOrThrowAsync(
            bookSnapshotRepository, integrationEvent.BookId, logger, cancellationToken);

        var feedItem = FeedItem.Create(
            actorId: profile.UserId,
            actorUsername: profile.Username,
            actorAvatarUrl: profile.AvatarUrl,
            activityType: ActivityType.BookFinished,
            targetType: null,
            referenceId: integrationEvent.BookId,
            bookTitle: book.Title,
            bookAuthor: book.AuthorDisplay,
            bookCoverUrl: book.CoverUrl,
            data: null);

        await feedItemRepository.StageAddAsync(feedItem, cancellationToken);

        logger.LogInformation(
            "Staged BookFinished FeedItem for user {UserId} book {BookId}",
            integrationEvent.UserId, integrationEvent.BookId);
    }
}
