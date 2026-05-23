using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Social.Infrastructure.Persistence.Repositories;

public class BookSnapshotRepository : IBookSnapshotRepository
{
    private readonly SocialDbContext _context;

    public BookSnapshotRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<BookSnapshot?> GetByBookIdAsync(
        Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.BookSnapshots
            .FirstOrDefaultAsync(bs => bs.BookId == bookId, cancellationToken);
    }

    public async Task AddOrUpdateAsync(
        BookSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await StageAddOrUpdateAsyncCore(snapshot, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task StageAddOrUpdateAsync(
        BookSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        return StageAddOrUpdateAsyncCore(snapshot, cancellationToken);
    }

    private async Task StageAddOrUpdateAsyncCore(
        BookSnapshot snapshot, CancellationToken cancellationToken)
    {
        var existing = await _context.BookSnapshots
            .FirstOrDefaultAsync(bs => bs.BookId == snapshot.BookId, cancellationToken);

        if (existing is null)
        {
            await _context.BookSnapshots.AddAsync(snapshot, cancellationToken);
        }
        else
        {
            existing.Update(
                snapshot.Title,
                snapshot.AuthorDisplay,
                snapshot.CoverUrl,
                snapshot.PageCount);
        }
    }
}
