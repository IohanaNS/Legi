using Legi.Contracts.Identity;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Profiles.IntegrationEventHandlers;

/// <summary>
/// Social's consumer for <see cref="UserUsernameChangedIntegrationEvent"/>.
///
/// Updates every denormalized username snapshot that Social stores:
///   1. UserProfile.Username   — staged; commits atomically with the inbox row via
///      IntegrationEventDispatcher's single SaveChangesAsync (decision 8.1).
///   2. FeedItem.ActorUsername — ExecuteUpdateAsync (immediate bulk SQL, idempotent).
///   3. ContentSnapshot.OwnerUsername — ExecuteUpdateAsync (same rationale).
///   4. Notification.ActorUsername   — ExecuteUpdateAsync (same rationale).
///
/// The three bulk-update calls auto-commit outside the dispatcher's implicit
/// transaction, but re-delivery is harmless because setting the same username
/// again is a no-op in effect.
///
/// MUST NOT call SaveChangesAsync — the dispatcher owns that commit (decision 8.1).
/// </summary>
public sealed class UserUsernameChangedIntegrationEventHandler
    : INotificationHandler<UserUsernameChangedIntegrationEvent>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IFeedItemRepository _feedItemRepository;
    private readonly IContentSnapshotRepository _contentSnapshotRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<UserUsernameChangedIntegrationEventHandler> _logger;

    public UserUsernameChangedIntegrationEventHandler(
        IUserProfileRepository userProfileRepository,
        IFeedItemRepository feedItemRepository,
        IContentSnapshotRepository contentSnapshotRepository,
        INotificationRepository notificationRepository,
        ILogger<UserUsernameChangedIntegrationEventHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _feedItemRepository = feedItemRepository;
        _contentSnapshotRepository = contentSnapshotRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(
        UserUsernameChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var userId = integrationEvent.UserId;
        var newUsername = integrationEvent.NewUsername;

        await _userProfileRepository.StageUpdateUsernameAsync(userId, newUsername, cancellationToken);

        await _feedItemRepository.BulkUpdateActorUsernameAsync(userId, newUsername, cancellationToken);
        await _contentSnapshotRepository.BulkUpdateOwnerUsernameAsync(userId, newUsername, cancellationToken);
        await _notificationRepository.BulkUpdateActorUsernameAsync(userId, newUsername, cancellationToken);

        _logger.LogInformation(
            "Updated username snapshots for user {UserId} to '{NewUsername}'",
            userId,
            newUsername);
    }
}
