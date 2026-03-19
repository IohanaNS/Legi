using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IReadingPostRepository
{
    Task<ReadingProgress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ReadingProgress progress, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReadingProgress progress, CancellationToken cancellationToken = default);
    Task DeleteAsync(ReadingProgress progress, CancellationToken cancellationToken = default);
}