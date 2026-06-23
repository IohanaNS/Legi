using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Identity.Infrastructure.Persistence.Repositories;

public class MfaEmailCodeRepository(IdentityDbContext context) : IMfaEmailCodeRepository
{
    public async Task<MfaEmailCode?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.MfaEmailCodes
            .Where(c => c.UserId == userId && c.ConsumedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task IssueAsync(MfaEmailCode code, CancellationToken cancellationToken = default)
    {
        // Atomic upsert so a user only ever has one live code. A single ON CONFLICT statement
        // (keyed by the unique user_id index) is race-safe — a delete-then-insert pair would let
        // two concurrent issues (e.g. a double-clicked "resend") collide on the unique index.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO mfa_email_codes (
                id, user_id, code_hash, expires_at, attempt_count, consumed_at, created_at, updated_at)
            VALUES (
                {code.Id}, {code.UserId}, {code.CodeHash}, {code.ExpiresAt}, {code.AttemptCount},
                NULL, {code.CreatedAt}, {code.UpdatedAt})
            ON CONFLICT (user_id) DO UPDATE SET
                id = EXCLUDED.id,
                code_hash = EXCLUDED.code_hash,
                expires_at = EXCLUDED.expires_at,
                attempt_count = EXCLUDED.attempt_count,
                consumed_at = NULL,
                created_at = EXCLUDED.created_at,
                updated_at = EXCLUDED.updated_at
            """, cancellationToken);
    }

    public async Task UpdateAsync(MfaEmailCode code, CancellationToken cancellationToken = default)
    {
        context.MfaEmailCodes.Update(code);
        await context.SaveChangesAsync(cancellationToken);
    }
}
