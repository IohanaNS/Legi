using Legi.Contracts.Library;
using Legi.Social.Application.Feed.IntegrationEventHandlers;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Lists.IntegrationEventHandlers;

/// <summary>
/// Keeps a list's interactability in sync with its visibility. A list that
/// becomes public gets a <see cref="ContentSnapshot"/> (List); a list that
/// becomes private has its snapshot deleted, which transparently blocks further
/// likes/comments/follows (they gate on snapshot existence). Existing
/// likes/comments/follows are left in place — they reappear if the list is made
/// public again.
///
/// MUST NOT call SaveChangesAsync (decision 8.1) — the dispatcher owns the commit.
/// </summary>
public sealed class UserListUpdatedIntegrationEventHandler(
    IUserProfileRepository userProfileRepository,
    IContentSnapshotRepository contentSnapshotRepository,
    ILogger<UserListUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<UserListUpdatedIntegrationEvent>
{
    public async Task Handle(
        UserListUpdatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!integrationEvent.IsPublic)
        {
            await contentSnapshotRepository.StageDeleteByTargetAsync(
                InteractableType.List, integrationEvent.ListId, cancellationToken);

            logger.LogInformation(
                "List {ListId} is now private; removed its ContentSnapshot.",
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
