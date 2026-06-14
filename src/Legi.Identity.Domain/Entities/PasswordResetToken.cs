using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt.HasValue;
    public bool IsActive => !IsExpired && !IsUsed;

    internal PasswordResetToken(string tokenHash, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    internal void MarkUsed()
    {
        if (IsUsed)
            throw new DomainException("Reset token has already been used");

        UsedAt = DateTime.UtcNow;
    }
}
