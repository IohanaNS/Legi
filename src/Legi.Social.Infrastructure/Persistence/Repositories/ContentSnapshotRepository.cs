using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class ContentSnapshotRepository(SocialDbContext context) : IContentSnapshotRepository
{
    public async Task<ContentSnapshot?> GetByTargetAsync(
        InteractableType targetType, Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return await context.ContentSnapshots
            .FirstOrDefaultAsync(
                cs => cs.TargetType == targetType && cs.TargetId == targetId,
                cancellationToken);
    }

    public async Task AddOrUpdateAsync(
        ContentSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await context.ContentSnapshots
            .FirstOrDefaultAsync(
                cs => cs.TargetType == snapshot.TargetType && cs.TargetId == snapshot.TargetId,
                cancellationToken);

        if (existing is null)
        {
            await context.ContentSnapshots.AddAsync(snapshot, cancellationToken);
        }
        else
        {
            existing.UpdateOwner(snapshot.OwnerUsername, snapshot.OwnerAvatarUrl);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByTargetAsync(
        InteractableType targetType, Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await context.ContentSnapshots
            .FirstOrDefaultAsync(
                cs => cs.TargetType == targetType && cs.TargetId == targetId,
                cancellationToken);

        if (snapshot is not null)
        {
            context.ContentSnapshots.Remove(snapshot);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task StageAddOrUpdateAsync(
        ContentSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await context.ContentSnapshots
            .FirstOrDefaultAsync(
                cs => cs.TargetType == snapshot.TargetType && cs.TargetId == snapshot.TargetId,
                cancellationToken);

        if (existing is null)
        {
            await context.ContentSnapshots.AddAsync(snapshot, cancellationToken);
        }
        else
        {
            existing.UpdateOwner(snapshot.OwnerUsername, snapshot.OwnerAvatarUrl);
        }
    }

    public async Task StageDeleteByTargetAsync(
        InteractableType targetType, Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await context.ContentSnapshots
            .FirstOrDefaultAsync(
                cs => cs.TargetType == targetType && cs.TargetId == targetId,
                cancellationToken);

        if (snapshot is not null)
        {
            context.ContentSnapshots.Remove(snapshot);
        }
    }

    public Task BulkUpdateOwnerUsernameAsync(
        Guid ownerId, string newUsername, CancellationToken cancellationToken = default)
    {
        return context.ContentSnapshots
            .Where(cs => cs.OwnerId == ownerId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(cs => cs.OwnerUsername, newUsername),
                cancellationToken);
    }
}
