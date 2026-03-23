using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class FollowReadRepository(SocialDbContext context) : IFollowReadRepository
{
    public async Task<PaginatedList<FollowUserDto>> GetFollowersAsync(
        Guid userId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Users who follow userId
        var query = context.Follows
            .AsNoTracking()
            .Where(f => f.FollowingId == userId)
            .Join(
                context.UserProfiles,
                f => f.FollowerId,
                up => up.UserId,
                (f, up) => new { Follow = f, Profile = up });

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Follow.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FollowUserDto
            {
                UserId = x.Profile.UserId,
                Username = x.Profile.Username,
                AvatarUrl = x.Profile.AvatarUrl,
                Bio = x.Profile.Bio,
                IsFollowedByViewer = viewerUserId.HasValue &&
                    context.Follows.Any(vf =>
                        vf.FollowerId == viewerUserId.Value &&
                        vf.FollowingId == x.Profile.UserId)
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<FollowUserDto>(items, totalCount, page, pageSize);
    }

    public async Task<PaginatedList<FollowUserDto>> GetFollowingAsync(
        Guid userId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Users that userId follows
        var query = context.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId)
            .Join(
                context.UserProfiles,
                f => f.FollowingId,
                up => up.UserId,
                (f, up) => new { Follow = f, Profile = up });

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Follow.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FollowUserDto
            {
                UserId = x.Profile.UserId,
                Username = x.Profile.Username,
                AvatarUrl = x.Profile.AvatarUrl,
                Bio = x.Profile.Bio,
                IsFollowedByViewer = viewerUserId.HasValue &&
                    context.Follows.Any(vf =>
                        vf.FollowerId == viewerUserId.Value &&
                        vf.FollowingId == x.Profile.UserId)
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<FollowUserDto>(items, totalCount, page, pageSize);
    }
}
