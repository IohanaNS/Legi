using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Repositories;

public interface IActivityRepository
{
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);
    Task DeleteByReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default);
    Task DeleteByActorAsync(Guid actorId, CancellationToken cancellationToken = default);
}