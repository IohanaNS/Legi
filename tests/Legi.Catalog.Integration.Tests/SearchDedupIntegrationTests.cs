using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.Catalog.Infrastructure.Persistence;
using Legi.Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Integration.Tests;

/// <summary>
/// Verifies that <see cref="BookReadRepository.SearchAsync"/> collapses multiple
/// editions of the same work into a single result (the most-rated edition),
/// against a real Postgres — this exercises EF Core's translation of the
/// self-referencing "representative per work" predicate, which can't be checked
/// by the mocked unit tests.
///
/// Set <c>CATALOG_TEST_DB</c> to a live, migrated Catalog Postgres connection
/// string; tests skip otherwise so the default unit suite stays Docker-free.
/// </summary>
public class SearchDedupIntegrationTests
{
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IBookReadRepository, BookReadRepository>();
        return services.BuildServiceProvider();
    }

    [SkippableFact]
    public async Task SearchAsync_CollapsesEditionsOfSameWork_ToMostRatedRepresentative()
    {
        var conn = Environment.GetEnvironmentVariable("CATALOG_TEST_DB");
        Skip.If(string.IsNullOrWhiteSpace(conn), "CATALOG_TEST_DB not set");

        await using var sp = BuildProvider(conn!);

        // A unique title token so this search can't collide with other catalog data.
        var marker = "ZZDEDUP-" + Guid.NewGuid().ToString("N");
        Guid highRatedEditionId;

        using (var scope = sp.CreateScope())
        {
            var works = scope.ServiceProvider.GetRequiredService<IWorkRepository>();
            var books = scope.ServiceProvider.GetRequiredService<IBookRepository>();

            var work = Work.Create(WorkKey.Synthesize(marker, "Dedup Author"), marker);
            await works.AddAsync(work);

            // Edition A: more rated → should be the surviving representative.
            var editionA = Book.Create(
                Isbn.Create(NewIsbn13()), marker, [Author.Create("Dedup Author")], Guid.NewGuid());
            editionA.AssignWork(work.Id);
            editionA.RecalculateRating(4.0m, 5);
            await books.AddAsync(editionA);
            highRatedEditionId = editionA.Id;

            // Edition B: same work, no ratings.
            var editionB = Book.Create(
                Isbn.Create(NewIsbn13()), marker, [Author.Create("Dedup Author")], Guid.NewGuid());
            editionB.AssignWork(work.Id);
            await books.AddAsync(editionB);
        }

        using (var scope = sp.CreateScope())
        {
            var read = scope.ServiceProvider.GetRequiredService<IBookReadRepository>();

            var (results, totalCount) = await read.SearchAsync(
                searchTerm: marker,
                authorSlug: null,
                tagSlugs: null,
                minRating: null,
                pageNumber: 1,
                pageSize: 20,
                sortBy: BookSortBy.Relevance,
                sortDescending: true);

            // Two editions, one work → exactly one result, the most-rated edition.
            Assert.Equal(1, totalCount);
            Assert.Single(results);
            Assert.Equal(highRatedEditionId, results[0].Id);
        }
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
