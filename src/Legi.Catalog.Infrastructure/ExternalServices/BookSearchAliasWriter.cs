using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.ExternalServices;

/// <summary>
/// Persists query → book associations produced while processing an external
/// search job. Writes are idempotent via the (book_id, alias) unique index, so
/// re-running a job for the same query never duplicates rows.
/// </summary>
internal class BookSearchAliasWriter(CatalogDbContext context) : IBookSearchAliasWriter
{
    public async Task LinkAsync(
        string query,
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken = default)
    {
        var alias = ExternalBookSearchQueue.NormalizeQuery(query);
        if (string.IsNullOrWhiteSpace(alias) || bookIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var bookId in bookIds.Distinct())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO book_search_aliases (id, book_id, alias, created_at)
                 VALUES ({Guid.NewGuid()}, {bookId}, {alias}, {now})
                 ON CONFLICT (book_id, alias) DO NOTHING
                 """,
                cancellationToken);
        }
    }
}
