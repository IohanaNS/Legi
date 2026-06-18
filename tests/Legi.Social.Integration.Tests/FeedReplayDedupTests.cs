using Legi.Contracts.Library;
using Legi.Messaging.Inbox;
using Legi.Social.Application;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Legi.Social.Infrastructure.Persistence;
using Legi.Social.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Integration.Tests;

/// <summary>
/// The §8.1.3 acceptance gate for the Social feed consumers (Fase 6 6E.2). A
/// <c>FeedItem</c> has NO natural key (fresh Guid per Create), so the inbox — and
/// only the inbox — prevents a redelivered Library event from creating duplicate
/// feed items. The shared dispatcher's dedup is exercised by the Library/Catalog
/// gates, but Social's no-natural-key creation path had never been replayed.
///
/// Drives the real <see cref="IntegrationEventDispatcher{TContext}"/> against the
/// compose social-db. Set <c>SOCIAL_TEST_DB</c>; skips otherwise.
/// </summary>
public class FeedReplayDedupTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<SocialDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IBookSnapshotRepository, BookSnapshotRepository>();
        services.AddScoped<IFeedItemRepository, FeedItemRepository>();
        services.AddScoped<IContentSnapshotRepository, ContentSnapshotRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddSocialApplication(); // IMediator + the INotificationHandler<> consumers
        services.AddSingleton<IntegrationEventDispatcher<SocialDbContext>>();
        return services.BuildServiceProvider();
    }

    private static async Task<(Guid userId, Guid bookId)> SeedActorAndBookAsync(ServiceProvider sp)
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
        ctx.UserProfiles.Add(UserProfile.Create(userId, "actor" + userId.ToString("N")[..8]));
        ctx.BookSnapshots.Add(BookSnapshot.Create(bookId, "Dune", "Frank Herbert", null, null));
        await ctx.SaveChangesAsync();
        return (userId, bookId);
    }

    private static async Task DispatchBookAddedAsync(ServiceProvider sp, Guid messageId, Guid userId, Guid bookId)
    {
        var dispatcher = sp.GetRequiredService<IntegrationEventDispatcher<SocialDbContext>>();
        var evt = new BookAddedToLibraryIntegrationEvent(
            Guid.NewGuid(), userId, bookId, Wishlist: false, AddedAt: DateTime.UtcNow, WorkId: Guid.NewGuid());
        await dispatcher.DispatchAsync(
            messageId, typeof(BookAddedToLibraryIntegrationEvent).AssemblyQualifiedName!, evt);
    }

    private static async Task<int> FeedItemCountForActorAsync(ServiceProvider sp, Guid userId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
        return await ctx.FeedItems.AsNoTracking().CountAsync(fi => fi.ActorId == userId);
    }

    private static async Task<int> InboxRowCountAsync(ServiceProvider sp, Guid messageId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
        return await ctx.Set<InboxMessage>().CountAsync(m => m.Id == messageId);
    }

    [SkippableFact]
    public async Task BookAddedToLibrary_DeliveredTwiceWithSameMessageId_CreatesExactlyOneFeedItem()
    {
        var conn = Environment.GetEnvironmentVariable("SOCIAL_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "SOCIAL_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var (userId, bookId) = await SeedActorAndBookAsync(sp);
        var messageId = Guid.NewGuid();

        // Same envelope twice — a FeedItem has no natural key, so without inbox
        // dedup this would create two BookStarted items.
        await DispatchBookAddedAsync(sp, messageId, userId, bookId);
        await DispatchBookAddedAsync(sp, messageId, userId, bookId);

        Assert.Equal(1, await FeedItemCountForActorAsync(sp, userId));
        Assert.Equal(1, await InboxRowCountAsync(sp, messageId));

        // Attribution: a DISTINCT MessageId IS processed → a second FeedItem appears.
        // This proves the inbox MessageId is the only thing preventing duplicates
        // (there is no natural key to fall back on).
        await DispatchBookAddedAsync(sp, Guid.NewGuid(), userId, bookId);
        Assert.Equal(2, await FeedItemCountForActorAsync(sp, userId));
    }
}
