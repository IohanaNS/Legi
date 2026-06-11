using Legi.Contracts.Library;
using Legi.Social.Application.Feed.IntegrationEventHandlers;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Lists.IntegrationEventHandlers;

/// <summary>
/// Projects a Library list into Social. A list is interactable
/// (likeable/commentable/followable) only while it is public, so a
/// <see cref="ContentSnapshot"/> (List) is created only for public lists. The
/// snapshot's existence is what the like/comment/follow handlers gate on, so a
/// private list is transparently non-interactable.
///
/// MUST NOT call SaveChangesAsync (decision 8.1) — the dispatcher owns the commit.
/// </summary>
public sealed class UserListCreatedIntegrationEventHandler(
    IUserProfileRepository userProfileRepository,
    IContentSnapshotRepository contentSnapshotRepository,
    ILogger<UserListCreatedIntegrationEventHandler> logger)
    : INotificationHandler<UserListCreatedIntegrationEvent>
{
    public async Task Handle(
        UserListCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!integrationEvent.IsPublic)
        {
            logger.LogDebug(
                "List {ListId} created private; no ContentSnapshot projected.",
                integrationEvent.ListId);
            return;
        }

        var profile = await FeedLookups.GetProfileOrThrowAsync(
            userProfileRepository, integrationEvent.OwnerId, logger, cancellationToken);

        var snapshot = ContentSnapshot.Create(
            targetType: InteractableType.List,
            targetId: integrationEvent.ListId,
            ownerId: profile.UserId,
            ownerUsername: profile.Username,
            ownerAvatarUrl: profile.AvatarUrl,
            bookTitle: null,
            bookAuthor: null,
            bookCoverUrl: null,
            contentPreview: integrationEvent.Name);

        await contentSnapshotRepository.StageAddOrUpdateAsync(snapshot, cancellationToken);

        logger.LogInformation(
            "Staged ContentSnapshot for public list {ListId} (owner {OwnerId}).",
            integrationEvent.ListId, integrationEvent.OwnerId);
    }
}
