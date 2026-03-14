using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class UserBookRepository : IUserBookRepository
{
    private readonly LibraryDbContext _context;

    public UserBookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<UserBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserBooks
            .FirstOrDefaultAsync(ub => ub.Id == id, cancellationToken);
    }

    public async Task<UserBook?> GetByUserAndBookAsync(
        Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        // Global query filter already excludes soft-deleted
        return await _context.UserBooks
            .FirstOrDefaultAsync(
                ub => ub.UserId == userId && ub.BookId == bookId,
                cancellationToken);
    }

    public async Task AddAsync(UserBook userBook, CancellationToken cancellationToken = default)
    {
        await _context.UserBooks.AddAsync(userBook, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserBook userBook, CancellationToken cancellationToken = default)
    {
        _context.UserBooks.Update(userBook);
        await _context.SaveChangesAsync(cancellationToken);
    }
}