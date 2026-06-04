using Legi.Library.Infrastructure.Persistence;
using Legi.Messaging.Inbox;
using Legi.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Library.Integration.Tests;

/// <summary>
/// §6D.2 retention gate. Verifies <see cref="RetentionCleaner"/> prunes processed
/// outbox + consumed inbox rows past the cutoff while KEEPING poison outbox rows
/// (never processed) and recent rows. Runs against the compose library-db.
/// Set <c>LIBRARY_TEST_DB</c>; skips otherwise.
/// </summary>
public class RetentionCleanerTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<LibraryDbContext>(o => o.UseNpgsql(connectionString));
        return services.BuildServiceProvider();
    }

    [SkippableFact]
    public async Task Cleanup_PrunesOldProcessed_KeepsPoisonAndRecent()
    {
        var conn = Environment.GetEnvironmentVariable("LIBRARY_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "LIBRARY_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var now = DateTime.UtcNow;
        var old = now - TimeSpan.FromDays(30);

        var oldProcessed = Guid.NewGuid();   // processed long ago → pruned
        var poison = Guid.NewGuid();          // never processed (poison) → kept
        var recentProcessed = Guid.NewGuid(); // processed just now → kept
        var oldInbox = Guid.NewGuid();        // consumed long ago → pruned
        var recentInbox = Guid.NewGuid();     // consumed just now → kept

        await using (var seed = new LibraryDbContext(
            new DbContextOptionsBuilder<LibraryDbContext>().UseNpgsql(conn!).Options))
        {
            seed.Set<OutboxMessage>().AddRange(
                new OutboxMessage { Id = oldProcessed, Type = "T", Payload = "{}", OccurredAt = old, ProcessedAt = old, NextRetryAt = old },
                new OutboxMessage { Id = poison, Type = "T", Payload = "{}", OccurredAt = old, ProcessedAt = null, Attempts = 5, Error = "boom", NextRetryAt = old },
                new OutboxMessage { Id = recentProcessed, Type = "T", Payload = "{}", OccurredAt = now, ProcessedAt = now, NextRetryAt = now });
            seed.Set<InboxMessage>().AddRange(
                new InboxMessage { Id = oldInbox, Type = "T", ProcessedAt = old },
                new InboxMessage { Id = recentInbox, Type = "T", ProcessedAt = now });
            await seed.SaveChangesAsync();
        }

        var cutoff = now - TimeSpan.FromDays(7);
        await using (var run = new LibraryDbContext(
            new DbContextOptionsBuilder<LibraryDbContext>().UseNpgsql(conn!).Options))
        {
            await RetentionCleaner.CleanupAsync(run, cutoff);
        }

        await using var verify = new LibraryDbContext(
            new DbContextOptionsBuilder<LibraryDbContext>().UseNpgsql(conn!).Options);
        async Task<bool> OutboxExists(Guid id) => await verify.Set<OutboxMessage>().AnyAsync(m => m.Id == id);
        async Task<bool> InboxExists(Guid id) => await verify.Set<InboxMessage>().AnyAsync(m => m.Id == id);

        Assert.False(await OutboxExists(oldProcessed), "old processed outbox should be pruned");
        Assert.True(await OutboxExists(poison), "poison outbox (never processed) must be kept");
        Assert.True(await OutboxExists(recentProcessed), "recent processed outbox must be kept");
        Assert.False(await InboxExists(oldInbox), "old inbox should be pruned");
        Assert.True(await InboxExists(recentInbox), "recent inbox must be kept");
    }
}
