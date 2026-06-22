using FluentValidation;
using FluentValidation.Results;
using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ConfirmMfaSetup;

public class ConfirmMfaSetupCommandHandler(
    IUserRepository userRepository,
    ITotpService totpService,
    IMfaSecretProtector secretProtector,
    ISecureTokenFactory tokenFactory,
    ISecurityAuditLogger auditLogger)
    : IRequestHandler<ConfirmMfaSetupCommand, ConfirmMfaSetupResponse>
{
    public async Task<ConfirmMfaSetupResponse> Handle(ConfirmMfaSetupCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.MfaEnabled)
            throw new ConflictException("MFA is already enabled.");

        if (string.IsNullOrEmpty(user.TotpSecret))
            throw new ConflictException("Start MFA setup before confirming.");

        var secret = secretProtector.Unprotect(user.TotpSecret);
        if (!totpService.VerifyCode(secret, request.Code))
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Code), "That verification code is incorrect. Try again.")]);

        var displayCodes = MfaRecoveryCodeGenerator.Generate();
        var hashes = displayCodes
            .Select(code => tokenFactory.Hash(MfaRecoveryCodeGenerator.Normalize(code)))
            .ToList();

        user.ConfirmMfaEnrollment(hashes, DateTime.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken);

        auditLogger.Record(new SecurityAuditEvent(SecurityEventType.MfaEnabled, UserId: user.Id));

        return new ConfirmMfaSetupResponse(displayCodes);
    }
}
