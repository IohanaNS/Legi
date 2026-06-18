using Legi.Contracts.Identity;
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
/// §8.1 bulk-operations idempotency gate (Fase 6 6E.3). The UserDeleted cascade
/// hard-deletes user_books/user_lists/reading_posts via <c>ExecuteDeleteAsync</c>,
/// which commits OUTSIDE the dispatcher's inbox transaction. Its safety rests on
/// two independent guarantees: the inbox dedups redeliveries, AND a filtered bulk
/// delete is convergent (re-running deletes zero rows). This proves both against
/// the real compose library-db. Set <c>LIBRARY_TEST_DB</c>; skips otherwise.
/// </summary>
public class UserDeletedCascadeReplayTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<LibraryDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IUserBookRepository, UserBookRepository>();
        services.AddScoped<IReadingPostRepository, ReadingProgressRepository>();
        services.AddScoped<IUserListRepository, UserListRepository>();
        services.AddLibraryApplication();
        services.AddSingleton<IntegrationEventDispatcher<LibraryDbContext>>();
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> SeedUserDataAsync(ServiceProvider sp)
    {
        var userId = Guid.NewGuid();
        using var scope = sp.CreateScope();
        var userBooks = scope.ServiceProvider.GetRequiredService<IUserBookRepository>();
        var posts = scope.ServiceProvider.GetRequiredService<IReadingPostRepository>();
        var lists = scope.ServiceProvider.GetRequiredService<IUserListRepository>();

        var bookId = Guid.NewGuid();
        var userBook = UserBook.Create(userId, bookId, Guid.NewGuid());
        await userBooks.AddAsync(userBook);
        await userBooks.AddAsync(UserBook.Create(userId, Guid.NewGuid(), Guid.NewGuid()));
        await posts.AddAsync(ReadingProgress.Create(userBook.Id, userId, bookId, Guid.NewGuid(), "seed post", null));
        await lists.AddAsync(UserList.Create(userId, "My List", "desc"));
        return userId;
    }

    private static async Task DispatchUserDeletedAsync(ServiceProvider sp, Guid messageId, Guid userId)
    {
        var dispatcher = sp.GetRequiredService<IntegrationEventDispatcher<LibraryDbContext>>();
        var evt = new UserDeletedIntegrationEvent(userId, DateTime.UtcNow);
        await dispatcher.DispatchAsync(
            messageId, typeof(UserDeletedIntegrationEvent).AssemblyQualifiedName!, evt);
    }

    private static async Task<(int books, int posts, int lists)> CountUserDataAsync(ServiceProvider sp, Guid userId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        // IgnoreQueryFilters so soft-deleted user_books count too (the cascade wipes them).
        var books = await ctx.Set<UserBook>().IgnoreQueryFilters().CountAsync(x => x.UserId == userId);
        var posts = await ctx.Set<ReadingProgress>().CountAsync(x => x.UserId == userId);
        var lists = await ctx.Set<UserList>().CountAsync(x => x.UserId == userId);
        return (books, posts, lists);
    }

    private static async Task<int> InboxRowCountAsync(ServiceProvider sp, Guid messageId)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        return await ctx.Set<InboxMessage>().CountAsync(m => m.Id == messageId);
    }

    [SkippableFact]
    public async Task UserDeleted_DeliveredTwice_PurgesOnce_AndConvergesOnReplay()
    {
        var conn = Environment.GetEnvironmentVariable("LIBRARY_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "LIBRARY_TEST_DB not set");

        await using var sp = BuildProvider(conn!);
        var userId = await SeedUserDataAsync(sp);

        var before = await CountUserDataAsync(sp, userId);
        Assert.Equal((2, 1, 1), before);

        var messageId = Guid.NewGuid();
        // Same MessageId twice → inbox dedups the second; the bulk deletes run once.
        await DispatchUserDeletedAsync(sp, messageId, userId);
        await DispatchUserDeletedAsync(sp, messageId, userId);

        Assert.Equal((0, 0, 0), await CountUserDataAsync(sp, userId));
        Assert.Equal(1, await InboxRowCountAsync(sp, messageId));

        // A DISTINCT MessageId re-runs the cascade — convergent: deletes zero more,
        // final state unchanged (no error, no negative effects).
        await DispatchUserDeletedAsync(sp, Guid.NewGuid(), userId);
        Assert.Equal((0, 0, 0), await CountUserDataAsync(sp, userId));
    }
}
