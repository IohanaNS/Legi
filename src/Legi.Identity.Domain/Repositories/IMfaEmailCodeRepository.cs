using Legi.Identity.Domain.Entities;

namespace Legi.Identity.Domain.Repositories;

public interface IMfaEmailCodeRepository
{
    /// <summary>The user's most recent unconsumed code, or null. Usability is checked by the caller.</summary>
    Task<MfaEmailCode?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Stores a freshly issued code, removing any previous code for the same user first.</summary>
    Task IssueAsync(MfaEmailCode code, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to a tracked code (attempt increment or consumption).</summary>
    Task UpdateAsync(MfaEmailCode code, CancellationToken cancellationToken = default);
}
