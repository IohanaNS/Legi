using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class UserProfileReadRepository(SocialDbContext context) : IUserProfileReadRepository
{
    public async Task<IReadOnlyList<FollowUserDto>> SearchByUsernamePrefixAsync(
        string usernamePrefix,
        Guid? viewerUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = usernamePrefix.Trim().ToLowerInvariant();

        return await context.UserProfiles
            .AsNoTracking()
            .Where(profile => profile.Username.StartsWith(normalizedPrefix))
            .Where(profile => !viewerUserId.HasValue || profile.UserId != viewerUserId.Value)
            .OrderBy(profile => profile.Username)
            .Take(limit)
            .Select(profile => new FollowUserDto
            {
                UserId = profile.UserId,
                Username = profile.Username,
                AvatarUrl = profile.AvatarUrl,
                Bio = profile.Bio,
                IsFollowedByViewer = viewerUserId.HasValue &&
                    context.Follows.Any(follow =>
                        follow.FollowerId == viewerUserId.Value &&
                        follow.FollowingId == profile.UserId)
            })
            .ToListAsync(cancellationToken);
    }
}
