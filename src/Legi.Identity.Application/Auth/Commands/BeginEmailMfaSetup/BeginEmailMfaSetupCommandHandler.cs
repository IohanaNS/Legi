using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.BeginEmailMfaSetup;

public class BeginEmailMfaSetupCommandHandler(
    IUserRepository userRepository,
    IMfaEmailCodeRepository codeRepository,
    ISecureTokenFactory tokenFactory,
    IEmailSender emailSender,
    MfaSettings mfaSettings)
    : IRequestHandler<BeginEmailMfaSetupCommand, Unit>
{
    public async Task<Unit> Handle(BeginEmailMfaSetupCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.MfaEnabled)
            throw new ConflictException("MFA is already enabled.");

        // The code lands in the account inbox, so that inbox must be a confirmed, owned address.
        if (!user.IsEmailConfirmed)
            throw new ConflictException("Confirm your email address before enabling email-based MFA.");

        await MfaEmailCodeDispatcher.DispatchAsync(
            user, request.Language, codeRepository, tokenFactory, emailSender,
            mfaSettings.EmailCodeLifetimeMinutes, cancellationToken);

        return Unit.Value;
    }
}
