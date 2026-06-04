using Legi.Contracts.Social;
using Legi.Library.Application;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.Library.Infrastructure.Persistence;
using Legi.Library.Infrastructure.Persistence.Repositories;
using Legi.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Integration.Tests;

/// <summary>
/// The §8.1.1 acceptance gate for Phase 4E: proves that the inbox — and ONLY the
/// inbox — makes counter mutations idempotent under duplicate delivery. Library
/// holds no Like/Comment rows to dedup on, so a redelivered ContentLiked would
/// double-count if the inbox check failed.
///
/// This drives the real <see cref="IntegrationEventDispatcher{TContext}"/> against
/// a real Postgres (the docker-compose library-db), exercising the exact
/// consumer-side path: inbox check → mediator.Publish → single SaveChanges. It
/// bypasses only the RabbitMQ transport, which is irrelevant to dedup.
///
/// Requires a live, migrated Library Postgres. Set <c>LIBRARY_TEST_DB</c> to its
/// connection string; the tests skip otherwise (so the default unit suite stays
/// green without Docker).
/// </summary>
public class InboxReplayDedupTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<LibraryDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IReadingPostRepository, ReadingProgressRepository>();
        services.AddScoped<IBookSnapshotRepository, BookSnapshotRepository>();
        services.AddLibraryApplication(); // IMediator + the INotificationHandler<> consumers
        services.AddSingleton<IntegrationEventDispatcher<LibraryDbContext>>();
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> SeedPostAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var post = ReadingProgress.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "replay-dedup seed", null);
        ctx.ReadingPosts.Add(post);
        await ctx.SaveChangesAsync();
        return post.Id;
    }

    private static async Task<ReadingProgress> ReloadAsync(ServiceProvider sp, Guid postId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        return await ctx.ReadingPosts.AsNoTracking().FirstAsync(p => p.Id == postId);
    }

    private static async Task<int> InboxRowCountAsync(ServiceProvider sp, Guid messageId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        return await ctx.Set<InboxMessage>().CountAsync(m => m.Id == messageId);
    }

    [SkippableFact]
    public async Task ContentLiked_DeliveredTwiceWithSameMessageId_IncrementsLikesExactlyOnce()
    {
        var conn = Environment.GetEnvironmentVariable("LIBRARY_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "LIBRARY_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var postId = await SeedPostAsync(sp);
        var dispatcher = sp.GetRequiredService<IntegrationEventDispatcher<LibraryDbContext>>();

        var messageId = Guid.NewGuid();
        var evt = new ContentLikedIntegrationEvent("Post", postId, Guid.NewGuid());
        var typeName = typeof(ContentLikedIntegrationEvent).AssemblyQualifiedName!;

        // Two deliveries of the SAME envelope (same MessageId) — simulates a broker redelivery.
        await dispatcher.DispatchAsync(messageId, typeName, evt);
        await dispatcher.DispatchAsync(messageId, typeName, evt);

        // Moved exactly once, and exactly one inbox row exists for the MessageId.
        Assert.Equal(1, (await ReloadAsync(sp, postId)).LikesCount);
        Assert.Equal(1, await InboxRowCountAsync(sp, messageId));

        // Attribution: the guard is the MessageId (the inbox), not event content.
        // A DISTINCT MessageId carrying the same event IS processed → counter moves again.
        var differentMessageId = Guid.NewGuid();
        await dispatcher.DispatchAsync(differentMessageId, typeName, evt);

        Assert.Equal(2, (await ReloadAsync(sp, postId)).LikesCount);
    }

    [SkippableFact]
    public async Task ContentCommented_DeliveredTwiceWithSameMessageId_IncrementsCommentsExactlyOnce()
    {
        var conn = Environment.GetEnvironmentVariable("LIBRARY_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "LIBRARY_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var postId = await SeedPostAsync(sp);
        var dispatcher = sp.GetRequiredService<IntegrationEventDispatcher<LibraryDbContext>>();

        var messageId = Guid.NewGuid();
        var evt = new ContentCommentedIntegrationEvent("Post", postId, Guid.NewGuid(), Guid.NewGuid());
        var typeName = typeof(ContentCommentedIntegrationEvent).AssemblyQualifiedName!;

        await dispatcher.DispatchAsync(messageId, typeName, evt);
        await dispatcher.DispatchAsync(messageId, typeName, evt);

        // Same inbox guard, different event type → proves the dedup is event-agnostic.
        Assert.Equal(1, (await ReloadAsync(sp, postId)).CommentsCount);
        Assert.Equal(1, await InboxRowCountAsync(sp, messageId));
    }
}
