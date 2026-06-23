using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.SendMfaEmailCode;

public class SendMfaEmailCodeCommandHandler(
    IUserRepository userRepository,
    IMfaEmailCodeRepository codeRepository,
    IJwtTokenService jwtTokenService,
    ISecureTokenFactory tokenFactory,
    IEmailSender emailSender,
    MfaSettings mfaSettings)
    : IRequestHandler<SendMfaEmailCodeCommand, Unit>
{
    // Generic on purpose: never reveal whether the challenge, the user, or the method was the problem.
    private const string FailureMessage = "Invalid or expired MFA challenge.";

    public async Task<Unit> Handle(SendMfaEmailCodeCommand request, CancellationToken cancellationToken)
    {
        var userId = jwtTokenService.ValidateMfaChallengeToken(request.MfaToken);
        if (userId is null)
            throw new UnauthorizedException(FailureMessage);

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || !user.MfaEnabled || user.MfaMethod != MfaMethod.Email)
            throw new UnauthorizedException(FailureMessage);

        await MfaEmailCodeDispatcher.DispatchAsync(
            user, request.Language, codeRepository, tokenFactory, emailSender,
            mfaSettings.EmailCodeLifetimeMinutes, cancellationToken);

        return Unit.Value;
    }
}
