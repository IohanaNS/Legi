using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class ReadingPostRepository : IReadingPostRepository
{
    private readonly LibraryDbContext _context;

    public ReadingPostRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<ReadingProgress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReadingPosts
            .FirstOrDefaultAsync(rp => rp.Id == id, cancellationToken);
    }

    public async Task AddAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        await _context.ReadingPosts.AddAsync(progress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        _context.ReadingPosts.Update(progress);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        _context.ReadingPosts.Remove(progress);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
