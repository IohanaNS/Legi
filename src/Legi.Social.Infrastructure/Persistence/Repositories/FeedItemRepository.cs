using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class FeedItemRepository(SocialDbContext context) : IFeedItemRepository
{
    public async Task AddAsync(FeedItem feedItem, CancellationToken cancellationToken = default)
    {
        await context.FeedItems.AddAsync(feedItem, cancellationToken);
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
}
