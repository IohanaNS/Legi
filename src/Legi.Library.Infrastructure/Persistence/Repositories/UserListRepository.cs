using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class UserListRepository : IUserListRepository
{
    private readonly LibraryDbContext _context;

    public UserListRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<UserList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserLists
            .Include(ul => ul.Items)
            .FirstOrDefaultAsync(ul => ul.Id == id, cancellationToken);
    }

    public async Task AddAsync(UserList list, CancellationToken cancellationToken = default)
    {
        await _context.UserLists.AddAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserList list, CancellationToken cancellationToken = default)
    {
        _context.UserLists.Update(list);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserList list, CancellationToken cancellationToken = default)
    {
        _context.UserLists.Remove(list); // Cascade deletes UserListItems
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserLists
            .CountAsync(ul => ul.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByUserAndNameAsync(
        Guid userId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        return await _context.UserLists
            .AnyAsync(
                ul => ul.UserId == userId
                      && ul.Name.ToLower() == normalizedName,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserList>> GetListsContainingBookAsync(
        Guid userBookId, CancellationToken cancellationToken = default)
    {
        return await _context.UserLists
            .Include(ul => ul.Items)
            .Where(ul => ul.Items.Any(i => i.UserBookId == userBookId))
            .ToListAsync(cancellationToken);
    }
}
