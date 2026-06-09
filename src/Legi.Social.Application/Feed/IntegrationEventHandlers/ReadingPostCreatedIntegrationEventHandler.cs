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
/// Projects a Library reading post into the feed. A reading post is interactable
/// (it can be liked and commented on), so this handler creates BOTH:
/// <list type="bullet">
///   <item>a <see cref="ContentSnapshot"/> for (Post, PostId) carrying the owner
///         (for delete authorization) and book/preview context;</item>
///   <item>a <see cref="ActivityType.ProgressPosted"/> FeedItem keyed by the actor,
///         with progress + content baked into the Data JSON.</item>
/// </list>
///
/// Resolves actor + book display data from local read models (decisions 8.3 /
/// 2.6.1); a missing lookup throws to redeliver. MUST NOT call SaveChangesAsync
/// (decision 8.1).
/// </summary>
public sealed class ReadingPostCreatedIntegrationEventHandler(
    IUserProfileRepository userProfileRepository,
    IBookSnapshotRepository bookSnapshotRepository,
    IContentSnapshotRepository contentSnapshotRepository,
    IFeedItemRepository feedItemRepository,
    ILogger<ReadingPostCreatedIntegrationEventHandler> logger)
    : INotificationHandler<ReadingPostCreatedIntegrationEvent>
{
    // Omit null progress/content keys so the Data payload only carries what the post has.
    private static readonly JsonSerializerOptions DataJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task Handle(
        ReadingPostCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var profile = await FeedLookups.GetProfileOrThrowAsync(
            userProfileRepository, integrationEvent.UserId, logger, cancellationToken);
        var book = await FeedLookups.GetBookOrThrowAsync(
            bookSnapshotRepository, integrationEvent.BookId, logger, cancellationToken);

        var snapshot = ContentSnapshot.Create(
            targetType: InteractableType.Post,
            targetId: integrationEvent.PostId,
            ownerId: profile.UserId,
            ownerUsername: profile.Username,
            ownerAvatarUrl: profile.AvatarUrl,
            bookTitle: book.Title,
            bookAuthor: book.AuthorDisplay,
            bookCoverUrl: book.CoverUrl,
            contentPreview: integrationEvent.IsSpoiler ? null : integrationEvent.Content);

        await contentSnapshotRepository.StageAddOrUpdateAsync(snapshot, cancellationToken);

        var data = JsonSerializer.Serialize(
            new
            {
                progress = integrationEvent.ProgressValue,
                progressType = integrationEvent.ProgressType,
                content = integrationEvent.Content,
                isSpoiler = integrationEvent.IsSpoiler ? true : (bool?)null
            },
            DataJsonOptions);

        var feedItem = FeedItem.Create(
            actorId: profile.UserId,
            actorUsername: profile.Username,
            actorAvatarUrl: profile.AvatarUrl,
            activityType: ActivityType.ProgressPosted,
            targetType: InteractableType.Post,
            referenceId: integrationEvent.PostId,
            bookTitle: book.Title,
            bookAuthor: book.AuthorDisplay,
            bookCoverUrl: book.CoverUrl,
            data: data,
            bookId: integrationEvent.BookId);

        await feedItemRepository.StageAddAsync(feedItem, cancellationToken);

        logger.LogInformation(
            "Staged ProgressPosted FeedItem + ContentSnapshot for post {PostId} (user {UserId}, book {BookId})",
            integrationEvent.PostId, integrationEvent.UserId, integrationEvent.BookId);
    }
}
