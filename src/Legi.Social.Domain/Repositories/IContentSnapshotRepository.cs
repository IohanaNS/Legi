using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Repositories;

public interface IContentSnapshotRepository
{
    Task<ContentSnapshot?> GetByTargetAsync(InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(ContentSnapshot snapshot, CancellationToken cancellationToken = default);
    Task DeleteByTargetAsync(InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
}