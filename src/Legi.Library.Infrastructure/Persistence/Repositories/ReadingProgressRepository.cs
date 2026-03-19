using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class ReadingProgressRepository(LibraryDbContext context) : IReadingPostRepository
{
    public async Task<ReadingProgress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ReadingPosts
            .FirstOrDefaultAsync(rp => rp.Id == id, cancellationToken);
    }

    public async Task AddAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        await context.ReadingPosts.AddAsync(progress, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        context.ReadingPosts.Update(progress);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        context.ReadingPosts.Remove(progress);
        await context.SaveChangesAsync(cancellationToken);
    }
}
