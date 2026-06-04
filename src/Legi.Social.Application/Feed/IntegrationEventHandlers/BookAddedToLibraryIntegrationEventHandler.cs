using Legi.Contracts.Library;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Feed.IntegrationEventHandlers;

/// <summary>
/// Projects a Library "book added to library" event into the feed.
///
/// A real add (Wishlist == false) becomes a <see cref="ActivityType.BookStarted"/>
/// FeedItem keyed by the actor (fan-out on read — decision 3.12: one FeedItem per
/// activity, never one per follower). A wishlist add is private and produces no
/// feed activity — the handler no-ops but the dispatcher still commits the inbox
/// row so the message is acked, not redelivered.
///
/// No ContentSnapshot: a book-started activity is not interactable
/// (nothing to like or comment on).
///
/// Resolves actor display data from the local UserProfile and book display data
/// from the local BookSnapshot (decisions 8.3 / 2.6.1) — both populated by earlier
/// integration events. A missing lookup throws so the broker redelivers (transient);
/// we never insert a FeedItem with null actor/book data.
///
/// MUST NOT call SaveChangesAsync — the IntegrationEventDispatcher owns the commit
/// (decision 8.1).
/// </summary>
public sealed class BookAddedToLibraryIntegrationEventHandler(
    IUserProfileRepository userProfileRepository,
    IBookSnapshotRepository bookSnapshotRepository,
    IFeedItemRepository feedItemRepository,
    ILogger<BookAddedToLibraryIntegrationEventHandler> logger)
    : INotificationHandler<BookAddedToLibraryIntegrationEvent>
{
    public async Task Handle(
        BookAddedToLibraryIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (integrationEvent.Wishlist)
        {
            logger.LogDebug(
                "BookAddedToLibrary for user {UserId} book {BookId} is a wishlist add; no feed activity",
                integrationEvent.UserId, integrationEvent.BookId);
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
            activityType: ActivityType.BookStarted,
            targetType: null,
            referenceId: integrationEvent.BookId,
            bookTitle: book.Title,
            bookAuthor: book.AuthorDisplay,
            bookCoverUrl: book.CoverUrl,
            data: null);

        await feedItemRepository.StageAddAsync(feedItem, cancellationToken);

        logger.LogInformation(
            "Staged BookStarted FeedItem for user {UserId} book {BookId}",
            integrationEvent.UserId, integrationEvent.BookId);
    }
}
