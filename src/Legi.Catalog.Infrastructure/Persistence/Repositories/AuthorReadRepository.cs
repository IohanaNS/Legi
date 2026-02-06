using Legi.Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Persistence.Repositories;

public class AuthorReadRepository(CatalogDbContext context) : IAuthorReadRepository
{
    public async Task<IReadOnlyList<AuthorSearchResult>> SearchAsync(
        string searchTerm,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return [];

        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

        var authors = await context.Authors
            .Where(a => a.Name.ToLower().Contains(normalizedSearch) ||
                        a.Slug.Contains(normalizedSearch))
            .OrderByDescending(a => a.BooksCount)
            .ThenBy(a => a.Name)
            .Take(limit)
            .Select(a => new AuthorSearchResult(a.Name, a.Slug, a.BooksCount))
            .ToListAsync(cancellationToken);

        return authors;
    }

    public async Task<IReadOnlyList<AuthorSearchResult>> GetPopularAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var authors = await context.Authors
            .Where(a => a.BooksCount > 0)
            .OrderByDescending(a => a.BooksCount)
            .ThenBy(a => a.Name)
            .Take(limit)
            .Select(a => new AuthorSearchResult(a.Name, a.Slug, a.BooksCount))
            .ToListAsync(cancellationToken);

        return authors;
    }

    public async Task<AuthorSearchResult?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var author = await context.Authors
            .Where(a => a.Slug == slug.ToLowerInvariant())
            .Select(a => new AuthorSearchResult(a.Name, a.Slug, a.BooksCount))
            .FirstOrDefaultAsync(cancellationToken);

        return author;
    }
}