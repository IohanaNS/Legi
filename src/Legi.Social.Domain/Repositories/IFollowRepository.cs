using Legi.Social.Domain.Entities;

namespace Legi.Social.Domain.Repositories;

public interface IFollowRepository
{
    Task<Follow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Follow?> GetByPairAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken = default);
    Task AddAsync(Follow follow, CancellationToken cancellationToken = default);
    Task DeleteAsync(Follow follow, CancellationToken cancellationToken = default);
}