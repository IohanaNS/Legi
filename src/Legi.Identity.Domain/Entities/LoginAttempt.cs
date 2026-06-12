using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

public class LoginAttempt : BaseAuditableEntity
{
    public string Identifier { get; private set; } = null!;
    public int FailedAttempts { get; private set; }
    public DateTime? LastFailedLoginAt { get; private set; }
    public DateTime? LockoutEndsAt { get; private set; }

    private LoginAttempt() { }

    public static LoginAttempt Create(string identifier, DateTime utcNow)
    {
        return new LoginAttempt
        {
            Id = Guid.NewGuid(),
            Identifier = NormalizeIdentifier(identifier),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public static string NormalizeIdentifier(string identifier)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainException("Login identifier is required");

        return normalized;
    }

    public bool IsLockedOut(DateTime utcNow)
    {
        return LockoutEndsAt is not null && LockoutEndsAt > utcNow;
    }

    public void RecordFailedLogin(
        int maxFailedAttempts,
        TimeSpan failureWindow,
        TimeSpan lockoutDuration,
        DateTime utcNow)
    {
        if (maxFailedAttempts <= 0)
            throw new DomainException("Max failed login attempts must be greater than zero");

        if (failureWindow <= TimeSpan.Zero)
            throw new DomainException("Login failure window must be greater than zero");

        if (lockoutDuration <= TimeSpan.Zero)
            throw new DomainException("Login lockout duration must be greater than zero");

        if (IsLockedOut(utcNow))
            return;

        if (LockoutEndsAt <= utcNow)
        {
            FailedAttempts = 0;
            LockoutEndsAt = null;
        }

        if (LastFailedLoginAt is null || LastFailedLoginAt.Value.Add(failureWindow) <= utcNow)
        {
            FailedAttempts = 0;
        }

        FailedAttempts++;
        LastFailedLoginAt = utcNow;

        if (FailedAttempts >= maxFailedAttempts)
        {
            LockoutEndsAt = utcNow.Add(lockoutDuration);
        }

        UpdatedAt = utcNow;
    }
}
