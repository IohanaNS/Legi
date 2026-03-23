using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class FollowRepository(SocialDbContext context) : IFollowRepository
{
    public async Task<Follow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Follows
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<Follow?> GetByPairAsync(
        Guid followerId, Guid followingId, CancellationToken cancellationToken = default)
    {
        return await context.Follows
            .FirstOrDefaultAsync(
                f => f.FollowerId == followerId && f.FollowingId == followingId,
                cancellationToken);
    }

    public async Task AddAsync(Follow follow, CancellationToken cancellationToken = default)
    {
        await context.Follows.AddAsync(follow, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Follow follow, CancellationToken cancellationToken = default)
    {
        context.Follows.Remove(follow);
        await context.SaveChangesAsync(cancellationToken);
    }
}
