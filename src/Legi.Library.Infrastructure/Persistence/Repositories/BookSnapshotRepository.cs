using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class BookSnapshotRepository : IBookSnapshotRepository
{
    private readonly LibraryDbContext _context;

    public BookSnapshotRepository(LibraryDbContext context)
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

        await _context.SaveChangesAsync(cancellationToken);
    }
}
