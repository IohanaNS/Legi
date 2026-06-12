using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Identity.Infrastructure.Persistence.Repositories;

public class LoginAttemptRepository(IdentityDbContext context) : ILoginAttemptRepository
{
    public async Task<LoginAttempt?> GetByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var normalized = LoginAttempt.NormalizeIdentifier(identifier);

        return await context.LoginAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(la => la.Identifier == normalized, cancellationToken);
    }

    public async Task RecordFailedAttemptAsync(
        string identifier,
        int maxFailedAttempts,
        TimeSpan failureWindow,
        TimeSpan lockoutDuration,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var normalized = LoginAttempt.NormalizeIdentifier(identifier);
        var resetCutoff = utcNow.Subtract(failureWindow);
        var lockoutEndsAt = utcNow.Add(lockoutDuration);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO login_attempts (
                id,
                identifier,
                failed_attempts,
                last_failed_login_at,
                lockout_ends_at,
                created_at,
                updated_at)
            VALUES (
                {Guid.NewGuid()},
                {normalized},
                1,
                {utcNow},
                CASE WHEN {maxFailedAttempts} <= 1 THEN {lockoutEndsAt} ELSE NULL END,
                {utcNow},
                {utcNow})
            ON CONFLICT (identifier) DO UPDATE SET
                failed_attempts = CASE
                    WHEN login_attempts.lockout_ends_at IS NOT NULL
                         AND login_attempts.lockout_ends_at > {utcNow}
                        THEN login_attempts.failed_attempts
                    WHEN login_attempts.lockout_ends_at IS NOT NULL
                         AND login_attempts.lockout_ends_at <= {utcNow}
                        THEN 1
                    WHEN login_attempts.last_failed_login_at IS NULL
                         OR login_attempts.last_failed_login_at <= {resetCutoff}
                        THEN 1
                    ELSE login_attempts.failed_attempts + 1
                END,
                last_failed_login_at = CASE
                    WHEN login_attempts.lockout_ends_at IS NOT NULL
                         AND login_attempts.lockout_ends_at > {utcNow}
                        THEN login_attempts.last_failed_login_at
                    ELSE {utcNow}
                END,
                lockout_ends_at = CASE
                    WHEN login_attempts.lockout_ends_at IS NOT NULL
                         AND login_attempts.lockout_ends_at > {utcNow}
                        THEN login_attempts.lockout_ends_at
                    WHEN (
                        CASE
                            WHEN login_attempts.lockout_ends_at IS NOT NULL
                                 AND login_attempts.lockout_ends_at <= {utcNow}
                                THEN 1
                            WHEN login_attempts.last_failed_login_at IS NULL
                                 OR login_attempts.last_failed_login_at <= {resetCutoff}
                                THEN 1
                            ELSE login_attempts.failed_attempts + 1
                        END
                    ) >= {maxFailedAttempts}
                        THEN {lockoutEndsAt}
                    ELSE NULL
                END,
                updated_at = CASE
                    WHEN login_attempts.lockout_ends_at IS NOT NULL
                         AND login_attempts.lockout_ends_at > {utcNow}
                        THEN login_attempts.updated_at
                    ELSE {utcNow}
                END
            """, cancellationToken);
    }

    public async Task ClearAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var normalized = LoginAttempt.NormalizeIdentifier(identifier);

        await context.LoginAttempts
            .Where(la => la.Identifier == normalized)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
