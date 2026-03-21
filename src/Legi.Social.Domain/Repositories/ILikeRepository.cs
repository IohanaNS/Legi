using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Repositories;

public interface ILikeRepository
{
    Task<Like?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Like?> GetByUserAndTargetAsync(Guid userId, InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task AddAsync(Like like, CancellationToken cancellationToken = default);
    Task DeleteAsync(Like like, CancellationToken cancellationToken = default);
}