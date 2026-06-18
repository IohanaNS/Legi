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
/// Verifies that rating an edition rolls up to its <see cref="Work"/> aggregate
/// across editions, driven through the real <see cref="IntegrationEventDispatcher{TContext}"/>
/// against a live Catalog Postgres. Two editions of one work, rated by two users,
/// must produce a work-level average that reflects both.
///
/// Set <c>CATALOG_TEST_DB</c>; skips otherwise.
/// </summary>
public class WorkRatingAggregationIntegrationTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IBookRatingRepository, BookRatingRepository>();
        services.AddCatalogApplication();
        services.AddSingleton<IntegrationEventDispatcher<CatalogDbContext>>();
        return services.BuildServiceProvider();
    }

    [SkippableFact]
    public async Task RatingTwoEditionsOfOneWork_AggregatesToTheWork()
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "CATALOG_TEST_DB not set");

        await using var sp = BuildProvider(conn!);

        Guid workId, editionA, editionB;
        using (var scope = sp.CreateScope())
        {
            var works = scope.ServiceProvider.GetRequiredService<IWorkRepository>();
            var books = scope.ServiceProvider.GetRequiredService<IBookRepository>();

            var marker = "ZZWORKRATE-" + Guid.NewGuid().ToString("N");
            var work = Work.Create(WorkKey.Synthesize(marker, "Author"), marker);
            await works.AddAsync(work);
            workId = work.Id;

            var a = Book.Create(Isbn.Create(NewIsbn13()), marker, [Author.Create("Author")], Guid.NewGuid(), workId: workId);
            await books.AddAsync(a);
            editionA = a.Id;

            var b = Book.Create(Isbn.Create(NewIsbn13()), marker, [Author.Create("Author")], Guid.NewGuid(), workId: workId);
            await books.AddAsync(b);
            editionB = b.Id;
        }

        // User 1 rates edition A 8 half-stars (4.0); user 2 rates edition B 10 (5.0).
        await DispatchRatedAsync(sp, Guid.NewGuid(), editionA, Guid.NewGuid(), 8);
        await DispatchRatedAsync(sp, Guid.NewGuid(), editionB, Guid.NewGuid(), 10);

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var work = await ctx.Works.AsNoTracking().FirstAsync(w => w.Id == workId);

            // Weighted across the two editions: (4.0*1 + 5.0*1) / 2 = 4.5, count 2.
            Assert.Equal(2, work.RatingsCount);
            Assert.Equal(4.5m, work.AverageRating);
        }
    }

    private static async Task DispatchRatedAsync(
        ServiceProvider sp, Guid messageId, Guid bookId, Guid userId, int rating)
    {
        var dispatcher = sp.GetRequiredService<IntegrationEventDispatcher<CatalogDbContext>>();
        var evt = new UserBookRatedIntegrationEvent(bookId, userId, rating, null, WorkId: Guid.NewGuid());
        await dispatcher.DispatchAsync(messageId, typeof(UserBookRatedIntegrationEvent).AssemblyQualifiedName!, evt);
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
