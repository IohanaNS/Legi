using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IUserListRepository
{
    Task<UserList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(UserList list, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserList list, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserList list, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserList>> GetListsContainingBookAsync(Guid userBookId,
        CancellationToken cancellationToken = default);
}