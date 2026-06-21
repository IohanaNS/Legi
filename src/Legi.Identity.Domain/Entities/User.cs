using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

public class User : BaseAuditableEntity
{
    public Email Email { get; private set; } = null!;
    public Username Username { get; private set; } = null!;
    public string? PasswordHash { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LastFailedLoginAt { get; private set; }
    public DateTime? LoginLockoutEndsAt { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }
    public bool IsEmailConfirmed => EmailConfirmedAt.HasValue;

    public bool MfaEnabled { get; private set; }
    public string? TotpSecret { get; private set; } // encrypted at rest; set during enrollment
    public DateTime? MfaEnabledAt { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<PasswordResetToken> _passwordResetTokens = new();
    public IReadOnlyCollection<PasswordResetToken> PasswordResetTokens => _passwordResetTokens.AsReadOnly();

    private readonly List<EmailConfirmationToken> _emailConfirmationTokens = new();
    public IReadOnlyCollection<EmailConfirmationToken> EmailConfirmationTokens => _emailConfirmationTokens.AsReadOnly();

    private readonly List<ExternalLogin> _externalLogins = new();
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    private readonly List<MfaRecoveryCode> _mfaRecoveryCodes = new();
    public IReadOnlyCollection<MfaRecoveryCode> MfaRecoveryCodes => _mfaRecoveryCodes.AsReadOnly();

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

    public static User CreateFromExternalLogin(
        Email email,
        Username username,
        string provider,
        string providerKey,
        DateTime emailConfirmedAtUtc)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = null,
            EmailConfirmedAt = emailConfirmedAtUtc,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user._externalLogins.Add(new ExternalLogin(provider, providerKey));

        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Id, username.Value, email.Value));

        return user;
    }

    public void AddExternalLogin(string provider, string providerKey)
    {
        var alreadyLinked = _externalLogins.Any(l =>
            l.Provider == provider && l.ProviderKey == providerKey);

        if (alreadyLinked)
            return;

        _externalLogins.Add(new ExternalLogin(provider, providerKey));
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmEmailFromExternalProvider(DateTime utcNow)
    {
        if (EmailConfirmedAt.HasValue)
            return;

        EmailConfirmedAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// Links an external provider that has verified ownership of this account's email.
    /// If the account had never confirmed its email, any pre-existing local credential
    /// is untrusted (a pre-hijacking attacker could have created it before the real
    /// owner signed in via the provider): the password and existing sessions are revoked
    /// so only the verified email owner retains access. A previously confirmed account
    /// keeps its password and sessions (it's the same proven owner adding a login method).
    /// </summary>
    public void LinkVerifiedExternalLogin(string provider, string providerKey, DateTime utcNow)
    {
        if (!EmailConfirmedAt.HasValue)
        {
            PasswordHash = null;
            RevokeAllRefreshTokens();
        }

        AddExternalLogin(provider, providerKey);
        ConfirmEmailFromExternalProvider(utcNow);
        UpdatedAt = utcNow;
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

    public PasswordResetToken AddPasswordResetToken(string tokenHash, DateTime expiresAt)
    {
        foreach (var existing in _passwordResetTokens.Where(t => t.IsActive))
        {
            existing.MarkUsed();
        }

        var token = new PasswordResetToken(tokenHash, expiresAt);
        _passwordResetTokens.Add(token);

        UpdatedAt = DateTime.UtcNow;

        return token;
    }

    public void RedeemPasswordReset(string tokenHash, string newPasswordHash, DateTime utcNow)
    {
        var token = _passwordResetTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.IsActive);

        if (token is null)
            throw new DomainException("Invalid or expired reset token");

        token.MarkUsed();
        UpdatePassword(newPasswordHash);
    }

    public EmailConfirmationToken AddEmailConfirmationToken(string tokenHash, DateTime expiresAt)
    {
        foreach (var existing in _emailConfirmationTokens.Where(t => t.IsActive))
        {
            existing.MarkUsed();
        }

        var token = new EmailConfirmationToken(tokenHash, expiresAt);
        _emailConfirmationTokens.Add(token);

        UpdatedAt = DateTime.UtcNow;

        return token;
    }

    public void ConfirmEmail(string tokenHash, DateTime utcNow)
    {
        var token = _emailConfirmationTokens.FirstOrDefault(t =>
            t.TokenHash == tokenHash &&
            !t.IsUsed &&
            t.ExpiresAt > utcNow);

        if (token is null)
            throw new DomainException("Invalid or expired email confirmation token");

        token.MarkUsed();
        EmailConfirmedAt ??= utcNow;
        UpdatedAt = utcNow;
    }

    public void MarkEmailConfirmationTokenSent(string tokenHash, DateTime utcNow)
    {
        var token = _emailConfirmationTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (token is null)
            throw new DomainException("Confirmation token not found");

        token.MarkSent(utcNow);
        UpdatedAt = utcNow;
    }

    // ----- MFA (TOTP) -----

    /// <summary>
    /// Begins TOTP enrollment by storing the already-encrypted shared secret. MFA stays
    /// inactive until <see cref="ConfirmMfaEnrollment"/> verifies the user can produce a code.
    /// </summary>
    public void StartMfaEnrollment(string encryptedSecret, DateTime utcNow)
    {
        if (MfaEnabled)
            throw new DomainException("MFA is already enabled");

        if (string.IsNullOrWhiteSpace(encryptedSecret))
            throw new DomainException("Encrypted secret is required");

        TotpSecret = encryptedSecret;
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// Activates MFA after the enrollment code was verified, storing the freshly generated
    /// recovery-code hashes (replacing any previous ones).
    /// </summary>
    public void ConfirmMfaEnrollment(IEnumerable<string> recoveryCodeHashes, DateTime utcNow)
    {
        if (MfaEnabled)
            throw new DomainException("MFA is already enabled");

        if (string.IsNullOrWhiteSpace(TotpSecret))
            throw new DomainException("No MFA enrollment is in progress");

        _mfaRecoveryCodes.Clear();
        foreach (var hash in recoveryCodeHashes)
            _mfaRecoveryCodes.Add(new MfaRecoveryCode(hash));

        MfaEnabled = true;
        MfaEnabledAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Disables MFA and clears the secret and all recovery codes.</summary>
    public void DisableMfa(DateTime utcNow)
    {
        if (!MfaEnabled)
            throw new DomainException("MFA is not enabled");

        MfaEnabled = false;
        TotpSecret = null;
        MfaEnabledAt = null;
        _mfaRecoveryCodes.Clear();
        UpdatedAt = utcNow;
    }

    /// <summary>Marks a matching unused recovery code as consumed; returns false if none match.</summary>
    public bool TryConsumeRecoveryCode(string codeHash, DateTime utcNow)
    {
        var code = _mfaRecoveryCodes.FirstOrDefault(c => c.CodeHash == codeHash && !c.IsUsed);

        if (code is null)
            return false;

        code.MarkUsed(utcNow);
        UpdatedAt = utcNow;
        return true;
    }
}
