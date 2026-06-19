using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler(
    IUserRepository userRepository,
    ISecureTokenFactory tokenFactory)
    : IRequestHandler<ConfirmEmailCommand, Unit>
{
    private const string InvalidTokenMessage = "This confirmation link is invalid or has expired.";

    public async Task<Unit> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenFactory.Hash(request.Token);
        var confirmed = await userRepository.ConfirmEmailAsync(
            tokenHash,
            DateTime.UtcNow,
            cancellationToken);

        if (!confirmed)
            throw new NotFoundException(InvalidTokenMessage);

        return Unit.Value;
    }
}
