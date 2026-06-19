using System.Data;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Legi.Identity.Infrastructure.Persistence.Repositories;

public class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByEmailWithPasswordResetTokensAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.PasswordResetTokens)
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();

        return await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username.Value == normalizedUsername, cancellationToken);
    }

    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default)
    {
        var normalized = emailOrUsername.Trim().ToLowerInvariant();

        return await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email.Value == normalized || u.Username.Value == normalized, cancellationToken);
    }

    public async Task<User?> GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
        string emailOrUsername,
        CancellationToken cancellationToken = default)
    {
        var normalized = emailOrUsername.Trim().ToLowerInvariant();

        return await context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.EmailConfirmationTokens)
            .FirstOrDefaultAsync(u => u.Email.Value == normalized || u.Username.Value == normalized, cancellationToken);
    }

    public async Task<bool> RedeemPasswordResetTokenAsync(
        string tokenHash,
        string newPasswordHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => RedeemPasswordResetTokenInTransactionAsync(
            tokenHash,
            newPasswordHash,
            utcNow,
            cancellationToken));
    }

    private async Task<bool> RedeemPasswordResetTokenInTransactionAsync(
        string tokenHash,
        string newPasswordHash,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var resetToken = await context.Set<PasswordResetToken>()
            .FromSqlInterpolated($"""
                SELECT *
                FROM password_reset_tokens
                WHERE token_hash = {tokenHash}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (resetToken is null)
            return false;

        var userId = context.Entry(resetToken).Property<Guid>("UserId").CurrentValue;
        var user = await context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.PasswordResetTokens)
            .SingleAsync(u => u.Id == userId, cancellationToken);

        user.RedeemPasswordReset(tokenHash, newPasswordHash, utcNow);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ConfirmEmailAsync(
        string tokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => ConfirmEmailInTransactionAsync(
            tokenHash,
            utcNow,
            cancellationToken));
    }

    private async Task<bool> ConfirmEmailInTransactionAsync(
        string tokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var confirmationToken = await context.Set<EmailConfirmationToken>()
            .FromSqlInterpolated($"""
                SELECT *
                FROM email_confirmation_tokens
                WHERE token_hash = {tokenHash}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (confirmationToken is null)
            return false;

        var userId = context.Entry(confirmationToken).Property<Guid>("UserId").CurrentValue;
        var user = await context.Users
            .Include(u => u.EmailConfirmationTokens)
            .SingleAsync(u => u.Id == userId, cancellationToken);

        try
        {
            user.ConfirmEmail(tokenHash, utcNow);
        }
        catch (DomainException)
        {
            return false;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(
        string currentTokenHash,
        string newTokenHash,
        DateTime newTokenExpiresAt,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => RotateRefreshTokenInTransactionAsync(
            currentTokenHash,
            newTokenHash,
            newTokenExpiresAt,
            cancellationToken));
    }

    private async Task<RefreshTokenRotationResult> RotateRefreshTokenInTransactionAsync(
        string currentTokenHash,
        string newTokenHash,
        DateTime newTokenExpiresAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var currentToken = await context.Set<RefreshToken>()
            .FromSqlInterpolated($"""
                SELECT *
                FROM refresh_tokens
                WHERE token_hash = {currentTokenHash}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (currentToken is null)
            return RefreshTokenRotationResult.Invalid();

        var userId = context.Entry(currentToken).Property<Guid>("UserId").CurrentValue;
        var user = await context.Users
            .Include(u => u.RefreshTokens)
            .SingleAsync(u => u.Id == userId, cancellationToken);

        if (currentToken.IsRevoked)
        {
            user.RevokeAllRefreshTokens();
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RefreshTokenRotationResult.ReplayDetected();
        }

        if (currentToken.IsExpired)
            return RefreshTokenRotationResult.Invalid();

        if (!user.IsEmailConfirmed)
            return RefreshTokenRotationResult.Invalid();

        user.RevokeRefreshToken(currentTokenHash);
        user.AddRefreshToken(newTokenHash, newTokenExpiresAt);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RefreshTokenRotationResult.Success(user);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await context.Users
            .AnyAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
    }
}
