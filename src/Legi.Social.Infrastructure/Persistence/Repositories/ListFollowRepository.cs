using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class ListFollowRepository(SocialDbContext context) : IListFollowRepository
{
    public async Task<ListFollow?> GetByUserAndListAsync(
        Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        return await context.ListFollows
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ListId == listId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        return await context.ListFollows
            .AnyAsync(f => f.UserId == userId && f.ListId == listId, cancellationToken);
    }

    public async Task<int> CountByListAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        return await context.ListFollows
            .CountAsync(f => f.ListId == listId, cancellationToken);
    }

    public async Task AddAsync(ListFollow follow, CancellationToken cancellationToken = default)
    {
        await context.ListFollows.AddAsync(follow, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ListFollow follow, CancellationToken cancellationToken = default)
    {
        context.ListFollows.Remove(follow);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task StageDeleteByListAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var follows = await context.ListFollows
            .Where(f => f.ListId == listId)
            .ToListAsync(cancellationToken);

        if (follows.Count > 0)
        {
            context.ListFollows.RemoveRange(follows);
        }
    }
}
