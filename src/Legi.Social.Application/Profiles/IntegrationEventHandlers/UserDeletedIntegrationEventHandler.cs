using Legi.Contracts.Identity;
using Legi.Social.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Profiles.IntegrationEventHandlers;

/// <summary>
/// Social's consumer for <see cref="UserDeletedIntegrationEvent"/>. Delegates to
/// <see cref="IUserDataPurger"/> for the coordinated bulk purge of all the
/// deleted user's Social data: their profile, follows (both directions, with
/// follower-count adjustments on the other parties' profiles), likes, comments,
/// content snapshots, feed items, and others' interactions ON the user's content.
///
/// The purge is idempotent (order-dependent — see UserDataPurger). Uses bulk SQL,
/// which bypasses the change tracker, so this handler does not stage anything for
/// the dispatcher's SaveChangesAsync. The dispatcher still commits the inbox row.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, section 8.1 (bulk operations exception)
/// and section 6.2 (UserDeleted cascade table).
/// </summary>
public sealed class UserDeletedIntegrationEventHandler
    : INotificationHandler<UserDeletedIntegrationEvent>
{
    private readonly IUserDataPurger _purger;
    private readonly ILogger<UserDeletedIntegrationEventHandler> _logger;

    public UserDeletedIntegrationEventHandler(
        IUserDataPurger purger,
        ILogger<UserDeletedIntegrationEventHandler> logger)
    {
        _purger = purger;
        _logger = logger;
    }

    public async Task Handle(
        UserDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var result = await _purger.PurgeAsync(integrationEvent.UserId, cancellationToken);

        _logger.LogInformation(
            "Purged Social data for deleted user {UserId}: " +
            "profile={ProfileDeleted}, follows={FollowsDeleted} " +
            "(followers-- on {FollowerDec}, following-- on {FollowingDec}), " +
            "own likes={OwnLikes}, own comments={OwnComments}, content={Content}, " +
            "own feed items={OwnFeed}, own notifications={OwnNotifications}; " +
            "indirect: likes={IndLikes}, comments={IndComments}, feed items={IndFeed}, notifications={IndNotifications}",
            integrationEvent.UserId,
            result.ProfileDeleted,
            result.FollowsDeleted,
            result.FollowerCountsDecremented,
            result.FollowingCountsDecremented,
            result.OwnLikesDeleted,
            result.OwnCommentsDeleted,
            result.ContentSnapshotsDeleted,
            result.OwnFeedItemsDeleted,
            result.OwnNotificationsDeleted,
            result.IndirectLikesDeleted,
            result.IndirectCommentsDeleted,
            result.IndirectFeedItemsDeleted,
            result.IndirectNotificationsDeleted);
    }
}
