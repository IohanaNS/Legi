using Legi.Identity.Domain.Entities;

namespace Legi.Identity.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailWithPasswordResetTokensAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    Task<bool> RedeemPasswordResetTokenAsync(
        string tokenHash,
        string newPasswordHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
    Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(
        string currentTokenHash,
        string newTokenHash,
        DateTime newTokenExpiresAt,
        CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
