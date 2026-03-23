using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Legi.Social.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class LikeReadRepository(SocialDbContext context) : ILikeReadRepository
{
    public async Task<PaginatedList<LikeUserDto>> GetByTargetAsync(
        InteractableType targetType,
        Guid targetId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Likes
            .AsNoTracking()
            .Where(l => l.TargetType == targetType && l.TargetId == targetId)
            .Join(
                context.UserProfiles,
                l => l.UserId,
                up => up.UserId,
                (l, up) => new { Like = l, Profile = up });

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Like.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LikeUserDto
            {
                UserId = x.Profile.UserId,
                Username = x.Profile.Username,
                AvatarUrl = x.Profile.AvatarUrl,
                IsFollowedByViewer = viewerUserId.HasValue &&
                    context.Follows.Any(f =>
                        f.FollowerId == viewerUserId.Value &&
                        f.FollowingId == x.Profile.UserId)
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<LikeUserDto>(items, totalCount, page, pageSize);
    }
}
