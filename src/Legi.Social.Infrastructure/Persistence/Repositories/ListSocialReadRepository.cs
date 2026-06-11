using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Legi.Social.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class ListSocialReadRepository(SocialDbContext context) : IListSocialReadRepository
{
    public async Task<ListSocialStateDto> GetStateAsync(
        Guid listId,
        Guid? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await context.ContentSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(
                cs => cs.TargetType == InteractableType.List && cs.TargetId == listId,
                cancellationToken);

        // No snapshot means the list is private (or unknown to Social): not interactable.
        if (snapshot is null)
            return new ListSocialStateDto(listId, false, false, 0, 0, 0, false, false);

        var likesCount = await context.Likes
            .CountAsync(l => l.TargetType == InteractableType.List && l.TargetId == listId, cancellationToken);

        var commentsCount = await context.Comments
            .CountAsync(c => c.TargetType == InteractableType.List && c.TargetId == listId, cancellationToken);

        var followersCount = await context.ListFollows
            .CountAsync(f => f.ListId == listId, cancellationToken);

        var isLikedByMe = false;
        var isFollowedByMe = false;
        if (viewerUserId is { } viewer)
        {
            isLikedByMe = await context.Likes.AnyAsync(
                l => l.TargetType == InteractableType.List && l.TargetId == listId && l.UserId == viewer,
                cancellationToken);
            isFollowedByMe = await context.ListFollows.AnyAsync(
                f => f.ListId == listId && f.UserId == viewer, cancellationToken);
        }

        return new ListSocialStateDto(
            listId,
            IsInteractable: true,
            IsOwner: viewerUserId == snapshot.OwnerId,
            LikesCount: likesCount,
            CommentsCount: commentsCount,
            FollowersCount: followersCount,
            IsLikedByMe: isLikedByMe,
            IsFollowedByMe: isFollowedByMe);
    }

    public async Task<PaginatedList<FollowedListDto>> GetFollowedListsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.ListFollows
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FollowedListDto(f.ListId, f.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<FollowedListDto>(items, totalItems, page, pageSize);
    }
}
