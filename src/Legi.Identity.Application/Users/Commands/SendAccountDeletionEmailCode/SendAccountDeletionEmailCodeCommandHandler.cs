using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.SendAccountDeletionEmailCode;

public class SendAccountDeletionEmailCodeCommandHandler(
    IUserRepository userRepository,
    IMfaEmailCodeRepository codeRepository,
    ISecureTokenFactory tokenFactory,
    IEmailSender emailSender,
    MfaSettings mfaSettings)
    : IRequestHandler<SendAccountDeletionEmailCodeCommand, Unit>
{
    private const string FailureMessage = "Account deletion email verification is not available.";

    public async Task<Unit> Handle(
        SendAccountDeletionEmailCodeCommand request,
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
