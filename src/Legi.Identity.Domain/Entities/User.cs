using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

public class User : BaseAuditableEntity
{
    public Email Email { get; private set; } = null!;
    public Username Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LastFailedLoginAt { get; private set; }
    public DateTime? LoginLockoutEndsAt { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public static User Create(Email email, Username username, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Id, username.Value, email.Value));

        return user;
    }

    public RefreshToken AddRefreshToken(string tokenHash, DateTime expiresAt)
    {
        var activeTokens = _refreshTokens
            .Where(t => t.IsActive)
            .OrderBy(t => t.CreatedAt)
            .ToList();

        while (activeTokens.Count >= 5)
        {
            activeTokens.First().Revoke();
            activeTokens.RemoveAt(0);
        }

        var token = new RefreshToken(tokenHash, expiresAt);
        _refreshTokens.Add(token);

        UpdatedAt = DateTime.UtcNow;

        return token;
    }

    public bool IsLoginLockedOut(DateTime utcNow)
    {
        return LoginLockoutEndsAt is not null && LoginLockoutEndsAt > utcNow;
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

        if (IsLoginLockedOut(utcNow))
            return;

        if (LoginLockoutEndsAt <= utcNow)
        {
            FailedLoginAttempts = 0;
            LoginLockoutEndsAt = null;
        }

        if (LastFailedLoginAt is null || LastFailedLoginAt.Value.Add(failureWindow) <= utcNow)
        {
            FailedLoginAttempts = 0;
        }

        FailedLoginAttempts++;
        LastFailedLoginAt = utcNow;

        if (FailedLoginAttempts >= maxFailedAttempts)
        {
            LoginLockoutEndsAt = utcNow.Add(lockoutDuration);
        }

        UpdatedAt = utcNow;
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        FailedLoginAttempts = 0;
        LastFailedLoginAt = null;
        LoginLockoutEndsAt = null;
        UpdatedAt = utcNow;
    }

    public void RevokeRefreshToken(string tokenHash)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (token is null)
            throw new DomainException("Token not found");

        token.Revoke();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public RefreshToken? GetValidRefreshToken(string tokenHash)
    {
        return _refreshTokens.FirstOrDefault(t =>
            t.TokenHash == tokenHash && t.IsActive);
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
        RevokeAllRefreshTokens();
    }
}
