using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class FeedItemRepository(SocialDbContext context) : IFeedItemRepository
{
    public Task<FeedItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.FeedItems
            .FirstOrDefaultAsync(fi => fi.Id == id, cancellationToken);
    }

    public async Task AddAsync(FeedItem feedItem, CancellationToken cancellationToken = default)
    {
        await context.FeedItems.AddAsync(feedItem, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(FeedItem feedItem, CancellationToken cancellationToken = default)
    {
        context.FeedItems.Remove(feedItem);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByReferenceAsync(
        Guid referenceId, CancellationToken cancellationToken = default)
    {
        var items = await context.FeedItems
            .Where(fi => fi.ReferenceId == referenceId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            context.FeedItems.RemoveRange(items);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByActorAsync(
        Guid actorId, CancellationToken cancellationToken = default)
    {
        var items = await context.FeedItems
            .Where(fi => fi.ActorId == actorId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            context.FeedItems.RemoveRange(items);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StageAddAsync(FeedItem feedItem, CancellationToken cancellationToken = default)
    {
        return context.FeedItems.AddAsync(feedItem, cancellationToken).AsTask();
    }

    public async Task StageDeleteByReferenceAsync(
        Guid referenceId, CancellationToken cancellationToken = default)
    {
        var items = await context.FeedItems
            .Where(fi => fi.ReferenceId == referenceId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            context.FeedItems.RemoveRange(items);
        }
    }

    public Task BulkUpdateActorUsernameAsync(
        Guid actorId, string newUsername, CancellationToken cancellationToken = default)
    {
        return context.FeedItems
            .Where(fi => fi.ActorId == actorId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(fi => fi.ActorUsername, newUsername),
                cancellationToken);
    }
}
