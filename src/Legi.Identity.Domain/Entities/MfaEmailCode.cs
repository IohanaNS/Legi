using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

/// <summary>
/// A short-lived one-time code emailed to a user as a second factor (enrollment and
/// login challenge). Transient security state with its own table — not part of the
/// <see cref="User"/> aggregate (these churn). The plaintext code is never stored; only
/// its hash. A user has at most one active code at a time (issuing a new one supersedes
/// the old). Safe only because of the strict limits below: short expiry + attempt cap +
/// single use (a 6-digit code is a tiny space without them).
/// </summary>
public class MfaEmailCode : BaseAuditableEntity
{
    /// <summary>Wrong guesses allowed before the code is dead and a new one must be requested.</summary>
    public const int MaxAttempts = 5;

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt is not null;

    private MfaEmailCode() { }

    public static MfaEmailCode Issue(Guid userId, string codeHash, DateTime expiresAt, DateTime utcNow)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required");

        if (string.IsNullOrWhiteSpace(codeHash))
            throw new DomainException("Code hash is required");

        if (expiresAt <= utcNow)
            throw new DomainException("Expiry must be in the future");

        return new MfaEmailCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            AttemptCount = 0,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>True while the code can still be tried: not consumed, not expired, attempts left.</summary>
    public bool IsUsable(DateTime utcNow)
        => !IsConsumed && AttemptCount < MaxAttempts && ExpiresAt > utcNow;

    public void RegisterFailedAttempt(DateTime utcNow)
    {
        AttemptCount++;
        UpdatedAt = utcNow;
    }

    public void Consume(DateTime utcNow)
    {
        ConsumedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
