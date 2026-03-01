using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IBookSnapshotRepository
{
    Task<BookSnapshot?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(BookSnapshot snapshot, CancellationToken cancellationToken = default);
}