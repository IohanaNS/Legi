using Legi.Social.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence;

/// <summary>
/// Bulk purge of a deleted user's Social data. Every operation is a filtered
/// bulk SQL statement (ExecuteDeleteAsync / ExecuteUpdateAsync) — immediate
/// commit, outside the change tracker. The "integration handlers MUST NOT call
/// SaveChangesAsync" convention (decision 8.1) doesn't constrain this code:
/// bulk operations are the documented exemption, since they bypass the unit
/// of work entirely.
///
/// IDEMPOTENCY IS ORDER-DEPENDENT. The counter decrements (steps 3a/3b) MUST run
/// BEFORE the Follows delete (step 4a). On a redelivered message, the Follows
/// rows are already gone, so the counter subqueries return empty and nothing is
/// double-decremented. Reordering these would break idempotency on redelivery.
/// See MESSAGING-ARCHITECTURE-decisions.md, section 8.1 (decrement-paired-with-delete).
///
/// Individual deletes are naturally idempotent (filtered DELETE matches zero rows
/// on rerun). The whole procedure converges to the correct state on any number
/// of redeliveries.
///
/// Race window (accepted): between step 1 (owned-targets query) and steps 2/4
/// (deletes), a concurrent like/comment could land on about-to-be-deleted content
/// and survive as an orphan. The underlying content is being deleted anyway, so
/// the orphan points at nothing meaningful. A future periodic recompute job
/// (Phase 6) would clean these. Bounding this with a serializable transaction
/// would be true correctness but isn't justified at current scale.
/// </summary>
public sealed class UserDataPurger : IUserDataPurger
{
    private readonly SocialDbContext _context;

    public UserDataPurger(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<UserPurgeResult> PurgeAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // === 1. Find the user's content — the join key for indirect cleanup. ===
        // Likes/Comments/FeedItems on the user's content don't carry the owner's
        // id; ContentSnapshot.OwnerId is the bridge. (Indexed as of Phase 3E.)
        var ownedTargets = await _context.ContentSnapshots
            .Where(cs => cs.OwnerId == userId)
            .Select(cs => new { cs.TargetType, cs.TargetId })
            .ToListAsync(cancellationToken);

        // targetIds.Contains(...) translates to a SQL IN clause. Postgres handles
        // this fine up to tens of thousands of parameters; chunk if a single user
        // ever exceeds ~30k content items.
        var targetsByType = ownedTargets
            .GroupBy(t => t.TargetType)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TargetId).ToList());

        // === 2. Indirect cleanup: others' interactions ON the user's content. ===
        // Batched by target type to avoid one round-trip per content item.
        var indirectLikes = 0;
        var indirectComments = 0;
        var indirectFeedItems = 0;

        foreach (var (targetType, targetIds) in targetsByType)
        {
            indirectLikes += await _context.Likes
                .Where(l => l.TargetType == targetType && targetIds.Contains(l.TargetId))
                .ExecuteDeleteAsync(cancellationToken);

            indirectComments += await _context.Comments
                .Where(c => c.TargetType == targetType && targetIds.Contains(c.TargetId))
                .ExecuteDeleteAsync(cancellationToken);

            // FeedItem.TargetType is nullable; null rows never match a non-null
            // targetType, which is correct — they're not about owned content.
            indirectFeedItems += await _context.FeedItems
                .Where(fi => fi.TargetType == targetType && targetIds.Contains(fi.ReferenceId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // === 3. Counter adjustments on OTHER users' profiles. ===
        // MUST run before step 4a (the Follows delete). See class-level note.
        // Bulk ExecuteUpdateAsync bypasses UserProfile.DecrementFollowers/Following
        // domain guards — a pre-existing inconsistency (Follow row exists but
        // counter already 0) would produce -1. Accepted: read-model drift, fixed
        // by Phase 6 recompute.

        // 3a. For each user X was following, decrement X's contribution to their FollowersCount.
        var followingIds = _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId);

        var followerCountsDecremented = await _context.UserProfiles
            .Where(p => followingIds.Contains(p.UserId))
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.FollowersCount, p => p.FollowersCount - 1),
                cancellationToken);

        // 3b. For each user who was following X, decrement their FollowingCount.
        var followerIds = _context.Follows
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId);

        var followingCountsDecremented = await _context.UserProfiles
            .Where(p => followerIds.Contains(p.UserId))
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.FollowingCount, p => p.FollowingCount - 1),
                cancellationToken);

        // === 4. Direct deletes: the user's own rows. ===

        // 4a. Follows (both directions). MUST come after step 3.
        var followsDeleted = await _context.Follows
            .Where(f => f.FollowerId == userId || f.FollowingId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var ownLikesDeleted = await _context.Likes
            .Where(l => l.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var ownCommentsDeleted = await _context.Comments
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var contentSnapshotsDeleted = await _context.ContentSnapshots
            .Where(cs => cs.OwnerId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var ownFeedItemsDeleted = await _context.FeedItems
            .Where(fi => fi.ActorId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // Notifications, both directions: the user's own (RecipientId) and the ones
        // others received about this now-deleted user's actions (ActorId). Ordering
        // is unconstrained — no counters depend on notifications.
        var ownNotificationsDeleted = await _context.Notifications
            .Where(n => n.RecipientId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var indirectNotificationsDeleted = await _context.Notifications
            .Where(n => n.ActorId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var profileDeleted = await _context.UserProfiles
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return new UserPurgeResult(
            IndirectLikesDeleted: indirectLikes,
            IndirectCommentsDeleted: indirectComments,
            IndirectFeedItemsDeleted: indirectFeedItems,
            FollowerCountsDecremented: followerCountsDecremented,
            FollowingCountsDecremented: followingCountsDecremented,
            FollowsDeleted: followsDeleted,
            OwnLikesDeleted: ownLikesDeleted,
            OwnCommentsDeleted: ownCommentsDeleted,
            ContentSnapshotsDeleted: contentSnapshotsDeleted,
            OwnFeedItemsDeleted: ownFeedItemsDeleted,
            OwnNotificationsDeleted: ownNotificationsDeleted,
            IndirectNotificationsDeleted: indirectNotificationsDeleted,
            ProfileDeleted: profileDeleted);
    }
}
