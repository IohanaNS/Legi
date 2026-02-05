using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    internal RefreshToken(string tokenHash, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    internal void Revoke()
    {
        if (IsRevoked)
            throw new DomainException("Token has already been revoked");

        RevokedAt = DateTime.UtcNow;
    }
}