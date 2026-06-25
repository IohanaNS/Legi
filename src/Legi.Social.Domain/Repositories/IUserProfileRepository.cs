using Legi.Social.Domain.Entities;

namespace Legi.Social.Domain.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new profile in the change tracker if none exists for the user.
    /// No-op if a profile already exists — the UserRegistered event carries the
    /// registration-time username, which must not overwrite a later rename.
    /// Does not save; the IntegrationEventDispatcher owns the commit
    /// (see MESSAGING-ARCHITECTURE-decisions.md, decision 8.1).
    /// </summary>
    Task StageCreateIfMissingAsync(UserProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the profile for <paramref name="userId"/>, calls
    /// <see cref="UserProfile.UpdateUsername"/> on it, and marks it modified
    /// in the change tracker — without saving. No-op when the profile does not
    /// exist yet (the UserRegistered event will carry the correct username).
    /// Does not save; the IntegrationEventDispatcher owns the commit.
    /// </summary>
    Task StageUpdateUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default);
}