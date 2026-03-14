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

    public async Task<ReadingPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReadingPosts
            .FirstOrDefaultAsync(rp => rp.Id == id, cancellationToken);
    }

    public async Task AddAsync(ReadingPost post, CancellationToken cancellationToken = default)
    {
        await _context.ReadingPosts.AddAsync(post, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ReadingPost post, CancellationToken cancellationToken = default)
    {
        _context.ReadingPosts.Update(post);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ReadingPost post, CancellationToken cancellationToken = default)
    {
        _context.ReadingPosts.Remove(post);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
