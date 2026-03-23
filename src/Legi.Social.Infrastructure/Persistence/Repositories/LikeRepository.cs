using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class LikeRepository(SocialDbContext context) : ILikeRepository
{
    public async Task<Like?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Likes
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Like?> GetByUserAndTargetAsync(
        Guid userId, InteractableType targetType, Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return await context.Likes
            .FirstOrDefaultAsync(
                l => l.UserId == userId && l.TargetType == targetType && l.TargetId == targetId,
                cancellationToken);
    }

    public async Task AddAsync(Like like, CancellationToken cancellationToken = default)
    {
        await context.Likes.AddAsync(like, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Like like, CancellationToken cancellationToken = default)
    {
        context.Likes.Remove(like);
        await context.SaveChangesAsync(cancellationToken);
    }
}
