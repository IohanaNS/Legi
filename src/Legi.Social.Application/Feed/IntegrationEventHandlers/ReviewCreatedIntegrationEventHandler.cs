using System.Text.Json;
using System.Text.Json.Serialization;
using Legi.Contracts.Library;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Feed.IntegrationEventHandlers;

/// <summary>
/// Projects a Library book review into the feed. A review is interactable (it can
/// be liked and commented on), so this handler creates BOTH:
/// <list type="bullet">
///   <item>a <see cref="ContentSnapshot"/> for (Review, ReviewId) carrying the owner
///         (for delete authorization) and book/preview context;</item>
///   <item>a <see cref="ActivityType.ReviewCreated"/> FeedItem keyed by the actor,
///         with the rating + content baked into the Data JSON and <c>BookId</c> set
///         so the book details page can query reviews by book.</item>
/// </list>
///
/// Mirrors <see cref="ReadingPostCreatedIntegrationEventHandler"/>. Resolves actor +
/// book display data from local read models; a missing lookup throws to redeliver.
/// MUST NOT call SaveChangesAsync (decision 8.1).
/// </summary>
public sealed class ReviewCreatedIntegrationEventHandler(
    IUserProfileRepository userProfileRepository,
    IBookSnapshotRepository bookSnapshotRepository,
    IContentSnapshotRepository contentSnapshotRepository,
    IFeedItemRepository feedItemRepository,
    ILogger<ReviewCreatedIntegrationEventHandler> logger)
    : INotificationHandler<ReviewCreatedIntegrationEvent>
{
    private static readonly JsonSerializerOptions DataJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task Handle(
        ReviewCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var profile = await FeedLookups.GetProfileOrThrowAsync(
            userProfileRepository, integrationEvent.UserId, logger, cancellationToken);
        var book = await FeedLookups.GetBookOrThrowAsync(
            bookSnapshotRepository, integrationEvent.BookId, logger, cancellationToken);

        var snapshot = ContentSnapshot.Create(
            targetType: InteractableType.Review,
            targetId: integrationEvent.ReviewId,
            ownerId: profile.UserId,
            ownerUsername: profile.Username,
            ownerAvatarUrl: profile.AvatarUrl,
            bookTitle: book.Title,
            bookAuthor: book.AuthorDisplay,
            bookCoverUrl: book.CoverUrl,
            contentPreview: integrationEvent.IsSpoiler ? null : integrationEvent.Content);

        await contentSnapshotRepository.StageAddOrUpdateAsync(snapshot, cancellationToken);

        // Contract carries the rating as a half-star integer (1-10); the feed Data
        // convention stores the display value (0.5-5.0 stars).
        var data = JsonSerializer.Serialize(
            new
            {
                rating = integrationEvent.Stars / 2.0,
                content = integrationEvent.Content,
                isSpoiler = integrationEvent.IsSpoiler ? true : (bool?)null
            },
            DataJsonOptions);

        var feedItem = FeedItem.Create(
            actorId: profile.UserId,
            actorUsername: profile.Username,
            actorAvatarUrl: profile.AvatarUrl,
            activityType: ActivityType.ReviewCreated,
            targetType: InteractableType.Review,
            referenceId: integrationEvent.ReviewId,
            bookTitle: book.Title,
            bookAuthor: book.AuthorDisplay,
            bookCoverUrl: book.CoverUrl,
            data: data,
            bookId: integrationEvent.BookId);

        await feedItemRepository.StageAddAsync(feedItem, cancellationToken);

        logger.LogInformation(
            "Staged ReviewCreated FeedItem + ContentSnapshot for review {ReviewId} (user {UserId}, book {BookId})",
            integrationEvent.ReviewId, integrationEvent.UserId, integrationEvent.BookId);
    }
}
