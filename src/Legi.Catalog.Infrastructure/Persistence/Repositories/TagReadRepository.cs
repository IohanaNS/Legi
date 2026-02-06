using Legi.Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Persistence.Repositories;

public class TagReadRepository(CatalogDbContext context) : ITagReadRepository
{
    public async Task<IReadOnlyList<TagSearchResult>> SearchAsync(
        string searchTerm, 
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<TagSearchResult>();

        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

        var tags = await context.Tags
            .Where(t => t.Name.ToLower().Contains(normalizedSearch) || 
                        t.Slug.Contains(normalizedSearch))
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .Take(limit)
            .Select(t => new TagSearchResult(t.Name, t.Slug, t.UsageCount))
            .ToListAsync(cancellationToken);

        return tags;
    }

    public async Task<IReadOnlyList<TagSearchResult>> GetPopularAsync(
        int limit = 20, 
        CancellationToken cancellationToken = default)
    {
        var tags = await context.Tags
            .Where(t => t.UsageCount > 0)
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .Take(limit)
            .Select(t => new TagSearchResult(t.Name, t.Slug, t.UsageCount))
            .ToListAsync(cancellationToken);

        return tags;
    }
}