using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

/// <summary>
/// A single-use MFA recovery code, stored only as a hash. Lets a user authenticate
/// when they lose access to their authenticator app.
/// </summary>
public class MfaRecoveryCode : BaseEntity
{
    public string CodeHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsUsed => UsedAt.HasValue;

    internal MfaRecoveryCode(string codeHash)
    {
        Id = Guid.NewGuid();
        CodeHash = codeHash;
        CreatedAt = DateTime.UtcNow;
    }

    internal void MarkUsed(DateTime utcNow)
    {
        if (IsUsed)
            throw new DomainException("Recovery code has already been used");

        UsedAt = utcNow;
    }
}
