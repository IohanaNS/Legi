using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.BeginMfaSetup;

public class BeginMfaSetupCommandHandler(
    IUserRepository userRepository,
    ITotpService totpService,
    IMfaSecretProtector secretProtector,
    MfaSettings mfaSettings)
    : IRequestHandler<BeginMfaSetupCommand, BeginMfaSetupResponse>
{
    public async Task<BeginMfaSetupResponse> Handle(BeginMfaSetupCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.MfaEnabled)
            throw new ConflictException("MFA is already enabled.");

        var secret = totpService.GenerateSecret();
        user.StartMfaEnrollment(secretProtector.Protect(secret), DateTime.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken);

        var otpAuthUri = totpService.BuildOtpAuthUri(secret, user.Email.Value, mfaSettings.Issuer);
        return new BeginMfaSetupResponse(secret, otpAuthUri);
    }
}
