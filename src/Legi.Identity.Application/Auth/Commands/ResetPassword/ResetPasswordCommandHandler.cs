using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenFactory tokenFactory,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand, Unit>
{
    private const string InvalidTokenMessage = "This reset link is invalid or has expired.";

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenFactory.Hash(request.Token);
        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var redeemed = await userRepository.RedeemPasswordResetTokenAsync(
            tokenHash,
            newPasswordHash,
            DateTime.UtcNow,
            cancellationToken);

        if (!redeemed)
            throw new NotFoundException(InvalidTokenMessage);

        return Unit.Value;
    }
}
