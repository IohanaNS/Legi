namespace Legi.Social.Application.Common.Interfaces;

/// <summary>
/// Coordinated bulk purge of all Social read-model data belonging to a deleted
/// user. This is NOT aggregate persistence — it's a cross-table cleanup of
/// read models (profiles, follows, likes, comments, content snapshots, feed
/// items), public profile media, plus follower-counter adjustments on OTHER users' profiles.
///
/// Implemented in Infrastructure because it issues bulk SQL (ExecuteDeleteAsync /
/// ExecuteUpdateAsync) directly against the DbContext. The ordering of operations
/// is load-bearing for idempotency — see the implementation.
/// </summary>
public interface IUserDataPurger
{
    Task<UserPurgeResult> PurgeAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Counts of rows affected by a user purge, for observability.
/// </summary>
public sealed record UserPurgeResult(
    int IndirectLikesDeleted,
    int IndirectCommentsDeleted,
    int IndirectFeedItemsDeleted,
    int FollowerCountsDecremented,
    int FollowingCountsDecremented,
    int FollowsDeleted,
    int OwnLikesDeleted,
    int OwnCommentsDeleted,
    int ContentSnapshotsDeleted,
    int OwnFeedItemsDeleted,
    int OwnNotificationsDeleted,
    int IndirectNotificationsDeleted,
    int ProfileDeleted);
