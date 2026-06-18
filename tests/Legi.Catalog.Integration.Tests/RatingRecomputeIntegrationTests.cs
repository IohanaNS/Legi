using Legi.Catalog.Application;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Repositories;
using Legi.Contracts.Library;
using Legi.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Integration.Tests;

/// <summary>
/// Phase 5 acceptance: drives the real <see cref="IntegrationEventDispatcher{TContext}"/>
/// (inbox check → mediator.Publish → single SaveChanges) against a real Catalog
/// Postgres, exercising the rated/removed consumers + the per-user-rows recompute
/// end-to-end. Bypasses only the RabbitMQ transport.
///
/// Covers: the §8.1.1-style replay/dedup gate (the part that matters), the
/// rate→re-rate→remove→remove-last recompute sequence, and Option-B convergence
/// (a non-deduped duplicate lands on the same value).
///
/// Set <c>CATALOG_TEST_DB</c> to a live, migrated Catalog Postgres connection
/// string; tests skip otherwise so the default unit suite stays Docker-free.
/// </summary>
public class RatingRecomputeIntegrationTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IBookRatingRepository, BookRatingRepository>();
        services.AddCatalogApplication(); // IMediator + the INotificationHandler<> consumers
        services.AddSingleton<IntegrationEventDispatcher<CatalogDbContext>>();
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> SeedBookAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var works = scope.ServiceProvider.GetRequiredService<IWorkRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<IBookRepository>();

        // Every book requires a work (FK). Create one (unique key per seed so the
        // works.work_key unique index isn't hit across tests).
        var work = Work.Create(
            WorkKey.Synthesize("Integration Test Book " + Guid.NewGuid().ToString("N"), "Test Author"),
            "Integration Test Book");
        await works.AddAsync(work);

        var book = Book.Create(
            Isbn.Create(NewIsbn13()),
            "Integration Test Book",
            [Author.Create("Test Author")],
            Guid.NewGuid(),
            workId: work.Id);
        await repo.AddAsync(book);
        return book.Id;
    }

    private static async Task DispatchRatedAsync(
        ServiceProvider sp, Guid messageId, Guid bookId, Guid userId, int rating, int? previous = null)
    {
        var dispatcher = sp.GetRequiredService<IntegrationEventDispatcher<CatalogDbContext>>();
        var evt = new UserBookRatedIntegrationEvent(bookId, userId, rating, previous, WorkId: Guid.NewGuid());
        await dispatcher.DispatchAsync(messageId, typeof(UserBookRatedIntegrationEvent).AssemblyQualifiedName!, evt);
    }

    private static async Task DispatchRemovedAsync(
        ServiceProvider sp, Guid messageId, Guid bookId, Guid userId, int removedRating)
    {
        var dispatcher = sp.GetRequiredService<IntegrationEventDispatcher<CatalogDbContext>>();
        var evt = new UserBookRatingRemovedIntegrationEvent(bookId, userId, removedRating, WorkId: Guid.NewGuid());
        await dispatcher.DispatchAsync(messageId, typeof(UserBookRatingRemovedIntegrationEvent).AssemblyQualifiedName!, evt);
    }

    private static async Task<(decimal Average, int Count)> ReloadAsync(ServiceProvider sp, Guid bookId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var book = await ctx.Books.AsNoTracking().FirstAsync(b => b.Id == bookId);
        return (book.AverageRating, book.RatingsCount);
    }

    private static async Task<int> InboxRowCountAsync(ServiceProvider sp, Guid messageId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await ctx.Set<InboxMessage>().CountAsync(m => m.Id == messageId);
    }

    [SkippableFact]
    public async Task UserBookRated_DeliveredTwiceWithSameMessageId_RecomputesExactlyOnce()
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "CATALOG_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var bookId = await SeedBookAsync(sp);
        var userA = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        // Same envelope (same MessageId) delivered twice — simulates a broker redelivery.
        await DispatchRatedAsync(sp, messageId, bookId, userA, rating: 8); // 8 half-stars → 4.0
        await DispatchRatedAsync(sp, messageId, bookId, userA, rating: 8);

        var afterReplay = await ReloadAsync(sp, bookId);
        Assert.Equal(4.0m, afterReplay.Average);
        Assert.Equal(1, afterReplay.Count); // one rating, not double-counted
        Assert.Equal(1, await InboxRowCountAsync(sp, messageId));

        // Attribution: a DISTINCT MessageId (different user) IS processed → aggregate moves.
        await DispatchRatedAsync(sp, Guid.NewGuid(), bookId, Guid.NewGuid(), rating: 10); // (8+10)/2 = 9 half → 4.5
        var afterSecond = await ReloadAsync(sp, bookId);
        Assert.Equal(4.5m, afterSecond.Average);
        Assert.Equal(2, afterSecond.Count);
    }

    [SkippableFact]
    public async Task RatingLifecycle_RateReRateRemove_TracksAverageAndCount()
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "CATALOG_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var bookId = await SeedBookAsync(sp);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        // A rates 8 → 4.0, count 1
        await DispatchRatedAsync(sp, Guid.NewGuid(), bookId, userA, 8);
        Assert.Equal((4.0m, 1), await ReloadAsync(sp, bookId));

        // A re-rates to 10 → 5.0, count UNCHANGED (upsert by (book, user))
        await DispatchRatedAsync(sp, Guid.NewGuid(), bookId, userA, 10, previous: 8);
        Assert.Equal((5.0m, 1), await ReloadAsync(sp, bookId));

        // B rates 6 → (10+6)/2 = 8 half → 4.0, count 2
        await DispatchRatedAsync(sp, Guid.NewGuid(), bookId, userB, 6);
        Assert.Equal((4.0m, 2), await ReloadAsync(sp, bookId));

        // Remove A → only B(6) → 3.0, count 1
        await DispatchRemovedAsync(sp, Guid.NewGuid(), bookId, userA, removedRating: 10);
        Assert.Equal((3.0m, 1), await ReloadAsync(sp, bookId));

        // Remove B (last rating) → 0, count 0
        await DispatchRemovedAsync(sp, Guid.NewGuid(), bookId, userB, removedRating: 6);
        Assert.Equal((0m, 0), await ReloadAsync(sp, bookId));
    }

    [SkippableFact]
    public async Task NonDedupedDuplicate_ConvergesToSameValue_OptionBSelfHeal()
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "CATALOG_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var bookId = await SeedBookAsync(sp);
        var userA = Guid.NewGuid();

        // A rates 8 (one message), then the SAME logical rating arrives again under a
        // DIFFERENT MessageId (a duplicate that slipped past the inbox). Upsert-by-key +
        // recompute-from-rows converge: the result is identical, no drift.
        await DispatchRatedAsync(sp, Guid.NewGuid(), bookId, userA, 8);
        await DispatchRatedAsync(sp, Guid.NewGuid(), bookId, userA, 8);

        Assert.Equal((4.0m, 1), await ReloadAsync(sp, bookId));
    }

    private static string NewIsbn13()
    {
        var rnd = Random.Shared;
        var body = "978" + string.Concat(Enumerable.Range(0, 9).Select(_ => rnd.Next(0, 10).ToString()));
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (i % 2 == 0 ? 1 : 3) * (body[i] - '0');
        var check = (10 - sum % 10) % 10;
        return body + check;
    }
}
