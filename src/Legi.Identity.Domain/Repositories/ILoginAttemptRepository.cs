using Legi.Identity.Domain.Entities;

namespace Legi.Identity.Domain.Repositories;

public interface ILoginAttemptRepository
{
    Task<LoginAttempt?> GetByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default);

    Task RecordFailedAttemptAsync(
        string identifier,
        int maxFailedAttempts,
        TimeSpan failureWindow,
        TimeSpan lockoutDuration,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        string identifier,
        CancellationToken cancellationToken = default);
}
