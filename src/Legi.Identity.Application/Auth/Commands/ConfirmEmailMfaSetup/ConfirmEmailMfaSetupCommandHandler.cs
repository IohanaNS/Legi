using FluentValidation;
using FluentValidation.Results;
using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ConfirmEmailMfaSetup;

public class ConfirmEmailMfaSetupCommandHandler(
    IUserRepository userRepository,
    IMfaEmailCodeRepository codeRepository,
    ISecureTokenFactory tokenFactory,
    ISecurityAuditLogger auditLogger)
    : IRequestHandler<ConfirmEmailMfaSetupCommand, ConfirmEmailMfaSetupResponse>
{
    public async Task<ConfirmEmailMfaSetupResponse> Handle(
        ConfirmEmailMfaSetupCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.MfaEnabled)
            throw new ConflictException("MFA is already enabled.");

        if (!user.IsEmailConfirmed)
            throw new ConflictException("Confirm your email address before enabling email-based MFA.");

        var now = DateTime.UtcNow;
        if (!await VerifyEmailCodeAsync(request.UserId, request.Code, now, cancellationToken))
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Code), "That code is invalid or has expired. Request a new one.")]);

        var displayCodes = MfaRecoveryCodeGenerator.Generate();
        var hashes = displayCodes
            .Select(code => tokenFactory.Hash(MfaRecoveryCodeGenerator.Normalize(code)))
            .ToList();

        user.EnableEmailMfa(hashes, now);
        await userRepository.UpdateAsync(user, cancellationToken);

        auditLogger.Record(new SecurityAuditEvent(SecurityEventType.MfaEnabled, UserId: user.Id, Detail: "email"));

        return new ConfirmEmailMfaSetupResponse(displayCodes);
    }

    private async Task<bool> VerifyEmailCodeAsync(Guid userId, string code, DateTime now, CancellationToken cancellationToken)
    {
        var active = await codeRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (active is null || !active.IsUsable(now))
            return false;

        var hash = tokenFactory.Hash(MfaEmailCodeGenerator.Normalize(code));
        if (active.CodeHash != hash)
        {
            active.RegisterFailedAttempt(now);
            await codeRepository.UpdateAsync(active, cancellationToken);
            return false;
        }

        active.Consume(now);
        await codeRepository.UpdateAsync(active, cancellationToken);
        return true;
    }
}
