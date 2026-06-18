using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Legi.Catalog.Infrastructure.Persistence.Repositories;
using Legi.Catalog.Infrastructure.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Catalog.Integration.Tests;

/// <summary>
/// §6D.3 gate. Verifies the on-demand <see cref="BookRatingReconciler"/> recomputes
/// a book's average from its <c>BookRating</c> rows (healing drift) and is a true
/// no-op when already correct (idempotent/convergent). Runs against the compose
/// catalog-db. Set <c>CATALOG_TEST_DB</c>; skips otherwise.
/// </summary>
public class BookRatingReconcilerTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<BookRatingReconciler>();
        return services.BuildServiceProvider();
    }

    [SkippableFact]
    public async Task ReconcileBook_HealsDriftedAverage_ThenNoOpsOnRerun()
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "CATALOG_TEST_DB not set");

        await using var sp = BuildProvider(conn!);

        // Seed a book + two ratings (8 and 10 half-stars → mean 9 → 4.5 on 0-5).
        var bookId = Guid.NewGuid();
        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var repo = scope.ServiceProvider.GetRequiredService<IBookRepository>();

            // Every book requires a work (FK).
            var work = Work.Create(
                WorkKey.Synthesize("Reconcile Probe " + Guid.NewGuid().ToString("N"), "Test Author"),
                "Reconcile Probe");
            ctx.Works.Add(work);
            await ctx.SaveChangesAsync();

            var book = Book.Create(Isbn.Create(NewIsbn13()), "Reconcile Probe", [Author.Create("Test Author")], Guid.NewGuid(), workId: work.Id);
            bookId = book.Id;
            await repo.AddAsync(book);

            ctx.BookRatings.AddRange(
                new BookRatingEntity { BookId = bookId, UserId = Guid.NewGuid(), Rating = 8, UpdatedAt = DateTime.UtcNow },
                new BookRatingEntity { BookId = bookId, UserId = Guid.NewGuid(), Rating = 10, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            // Corrupt the denormalized aggregate to simulate drift.
            var tracked = await ctx.Books.FirstAsync(b => b.Id == bookId);
            tracked.RecalculateRating(0.5m, 99);
            await ctx.SaveChangesAsync();
        }

        using (var scope = sp.CreateScope())
        {
            var reconciler = scope.ServiceProvider.GetRequiredService<BookRatingReconciler>();
            Assert.True(await reconciler.ReconcileBookAsync(bookId), "drifted book should be corrected");
        }

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var book = await ctx.Books.AsNoTracking().FirstAsync(b => b.Id == bookId);
            Assert.Equal(4.5m, book.AverageRating);
            Assert.Equal(2, book.RatingsCount);
        }

        using (var scope = sp.CreateScope())
        {
            var reconciler = scope.ServiceProvider.GetRequiredService<BookRatingReconciler>();
            Assert.False(await reconciler.ReconcileBookAsync(bookId), "already-correct book is a no-op");
        }
    }

    private static string NewIsbn13()
    {
        var rnd = Random.Shared;
        var body = "978" + string.Concat(Enumerable.Range(0, 9).Select(_ => rnd.Next(0, 10).ToString()));
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (i % 2 == 0 ? 1 : 3) * (body[i] - '0');
        return body + (10 - sum % 10) % 10;
    }
}
