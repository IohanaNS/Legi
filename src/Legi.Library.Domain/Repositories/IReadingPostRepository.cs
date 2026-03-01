using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IReadingPostRepository
{
    Task<ReadingPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ReadingPost post, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReadingPost post, CancellationToken cancellationToken = default);
    Task DeleteAsync(ReadingPost post, CancellationToken cancellationToken = default);
}