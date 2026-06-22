using FluentValidation;
using FluentValidation.Results;
using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.DisableMfa;

public class DisableMfaCommandHandler(
    IUserRepository userRepository,
    ITotpService totpService,
    IMfaSecretProtector secretProtector,
    ISecureTokenFactory tokenFactory,
    ISecurityAuditLogger auditLogger)
    : IRequestHandler<DisableMfaCommand, Unit>
{
    public async Task<Unit> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (!user.MfaEnabled)
            throw new ConflictException("MFA is not enabled.");

        var now = DateTime.UtcNow;

        // Require a current factor so a hijacked session alone cannot turn MFA off.
        if (!VerifyTotpOrRecoveryCode(user, request.Code, now))
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Code), "That verification or recovery code is invalid.")]);

        user.DisableMfa(now);
        await userRepository.UpdateAsync(user, cancellationToken);

        auditLogger.Record(new SecurityAuditEvent(SecurityEventType.MfaDisabled, UserId: user.Id));

        return Unit.Value;
    }

    private bool VerifyTotpOrRecoveryCode(User user, string code, DateTime now)
    {
        if (!string.IsNullOrEmpty(user.TotpSecret) &&
            totpService.VerifyCode(secretProtector.Unprotect(user.TotpSecret), code))
        {
            return true;
        }

        var hash = tokenFactory.Hash(MfaRecoveryCodeGenerator.Normalize(code));
        return user.TryConsumeRecoveryCode(hash, now);
    }
}
