using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IUserBookRepository
{
    Task<UserBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserBook?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
    Task AddAsync(UserBook userBook, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserBook userBook, CancellationToken cancellationToken = default);
}