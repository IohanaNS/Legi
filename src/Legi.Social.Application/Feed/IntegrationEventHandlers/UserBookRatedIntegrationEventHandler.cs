using System.Text.Json;
using Legi.Contracts.Library;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Feed.IntegrationEventHandlers;

/// <summary>
/// Projects a Library rating into the feed as a <see cref="ActivityType.BookRated"/>
/// FeedItem keyed by the actor. Not interactable, so no ContentSnapshot.
///
/// The contract carries the rating as a half-star integer (1–10). The feed Data
/// convention stores the display value (0.5–5.0 stars), so we halve it — matching
/// the FeedItem.Data examples in the domain model docs ({ "rating": 4.5 }).
///
/// Resolves actor + book display data from local read models (decisions 8.3 /
/// 2.6.1); a missing lookup throws to redeliver. MUST NOT call SaveChangesAsync
/// (decision 8.1).
/// </summary>
public sealed class UserBookRatedIntegrationEventHandler(
    IUserProfileRepository userProfileRepository,
    IBookSnapshotRepository bookSnapshotRepository,
    IFeedItemRepository feedItemRepository,
    ILogger<UserBookRatedIntegrationEventHandler> logger)
    : INotificationHandler<UserBookRatedIntegrationEvent>
{
    public async Task Handle(
        UserBookRatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var profile = await FeedLookups.GetProfileOrThrowAsync(
            userProfileRepository, integrationEvent.UserId, logger, cancellationToken);
        var book = await FeedLookups.GetBookOrThrowAsync(
            bookSnapshotRepository, integrationEvent.BookId, logger, cancellationToken);

        var displayRating = integrationEvent.Rating / 2.0;
        var data = JsonSerializer.Serialize(new { rating = displayRating });

        var feedItem = FeedItem.Create(
            actorId: profile.UserId,
            actorUsername: profile.Username,
            actorAvatarUrl: profile.AvatarUrl,
            activityType: ActivityType.BookRated,
            targetType: null,
            referenceId: integrationEvent.BookId,
            bookTitle: book.Title,
            bookAuthor: book.AuthorDisplay,
            bookCoverUrl: book.CoverUrl,
            data: data);

        await feedItemRepository.StageAddAsync(feedItem, cancellationToken);

        logger.LogInformation(
            "Staged BookRated FeedItem for user {UserId} book {BookId} ({Rating} stars)",
            integrationEvent.UserId, integrationEvent.BookId, displayRating);
    }
}
