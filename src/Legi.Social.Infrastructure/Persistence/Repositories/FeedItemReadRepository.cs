using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class FeedItemReadRepository(SocialDbContext context) : IFeedItemReadRepository
{
    public async Task<PaginatedList<FeedItemDto>> GetFeedAsync(
        Guid viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Feed = activities from people the viewer follows
        var query = context.FeedItems
            .AsNoTracking()
            .Where(fi => context.Follows.Any(f =>
                f.FollowerId == viewerUserId &&
                f.FollowingId == fi.ActorId));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(fi => fi.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(fi => new FeedItemDto
            {
                Id = fi.Id,
                ActorId = fi.ActorId,
                ActorUsername = fi.ActorUsername,
                ActorAvatarUrl = fi.ActorAvatarUrl,
                ActivityType = fi.ActivityType.ToString(),
                TargetType = fi.TargetType != null ? fi.TargetType.Value.ToString() : null,
                ReferenceId = fi.ReferenceId,
                BookTitle = fi.BookTitle,
                BookAuthor = fi.BookAuthor,
                BookCoverUrl = fi.BookCoverUrl,
                Data = fi.Data,
                // Real-time counts via subquery (same database, no cross-service call)
                LikesCount = fi.TargetType != null
                    ? context.Likes.Count(l =>
                        l.TargetType == fi.TargetType.Value &&
                        l.TargetId == fi.ReferenceId)
                    : 0,
                CommentsCount = fi.TargetType != null
                    ? context.Comments.Count(c =>
                        c.TargetType == fi.TargetType.Value &&
                        c.TargetId == fi.ReferenceId)
                    : 0,
                IsLikedByMe = fi.TargetType != null &&
                    context.Likes.Any(l =>
                        l.UserId == viewerUserId &&
                        l.TargetType == fi.TargetType.Value &&
                        l.TargetId == fi.ReferenceId),
                CreatedAt = fi.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<FeedItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PaginatedList<FeedItemDto>> GetUserActivityAsync(
        Guid targetUserId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.FeedItems
            .AsNoTracking()
            .Where(fi => fi.ActorId == targetUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(fi => fi.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(fi => new FeedItemDto
            {
                Id = fi.Id,
                ActorId = fi.ActorId,
                ActorUsername = fi.ActorUsername,
                ActorAvatarUrl = fi.ActorAvatarUrl,
                ActivityType = fi.ActivityType.ToString(),
                TargetType = fi.TargetType != null ? fi.TargetType.Value.ToString() : null,
                ReferenceId = fi.ReferenceId,
                BookTitle = fi.BookTitle,
                BookAuthor = fi.BookAuthor,
                BookCoverUrl = fi.BookCoverUrl,
                Data = fi.Data,
                LikesCount = fi.TargetType != null
                    ? context.Likes.Count(l =>
                        l.TargetType == fi.TargetType.Value &&
                        l.TargetId == fi.ReferenceId)
                    : 0,
                CommentsCount = fi.TargetType != null
                    ? context.Comments.Count(c =>
                        c.TargetType == fi.TargetType.Value &&
                        c.TargetId == fi.ReferenceId)
                    : 0,
                IsLikedByMe = viewerUserId.HasValue && fi.TargetType != null &&
                    context.Likes.Any(l =>
                        l.UserId == viewerUserId.Value &&
                        l.TargetType == fi.TargetType.Value &&
                        l.TargetId == fi.ReferenceId),
                CreatedAt = fi.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<FeedItemDto>(items, totalCount, page, pageSize);
    }
}
