using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.SendUsernameChangeEmailCode;

public class SendUsernameChangeEmailCodeCommandHandler(
    IUserRepository userRepository,
    IMfaEmailCodeRepository codeRepository,
    ISecureTokenFactory tokenFactory,
    IEmailSender emailSender,
    MfaSettings mfaSettings)
    : IRequestHandler<SendUsernameChangeEmailCodeCommand, Unit>
{
    private const string FailureMessage = "Username change email verification is not available.";

    public async Task<Unit> Handle(
        SendUsernameChangeEmailCodeCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.MfaEnabled || user.MfaMethod != MfaMethod.Email)
            throw new UnauthorizedException(FailureMessage);

        await MfaEmailCodeDispatcher.DispatchAsync(
            user,
            request.Language,
            codeRepository,
            tokenFactory,
            emailSender,
            mfaSettings.EmailCodeLifetimeMinutes,
            cancellationToken);

        return Unit.Value;
    }
}
