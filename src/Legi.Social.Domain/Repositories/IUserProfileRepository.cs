using Legi.Social.Domain.Entities;

namespace Legi.Social.Domain.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserProfile profile, CancellationToken cancellationToken = default);
}