using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Repositories;
using Legi.Identity.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.ChangeUsername;

public class ChangeUsernameCommandHandler(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<ChangeUsernameCommand, ChangeUsernameResponse>
{
    public async Task<ChangeUsernameResponse> Handle(
        ChangeUsernameCommand request,
        CancellationToken cancellationToken)
    {
        var tokenUserId = jwtTokenService.ValidateUsernameChangeChallengeToken(request.ChallengeToken);
        if (tokenUserId is null || tokenUserId.Value != request.UserId)
            throw new UnauthorizedException("Invalid or expired challenge token.");

        // DomainException on bad format — middleware maps this to 400
        var newUsername = Username.Create(request.NewUsername);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.Username.Value == newUsername.Value)
            return new ChangeUsernameResponse(newUsername.Value);

        if (await userRepository.ExistsWithUsernameAsync(newUsername.Value, cancellationToken))
            throw new ConflictException("A user with this username already exists.");

        user.ChangeUsername(newUsername);
        await userRepository.UpdateAsync(user, cancellationToken);

        return new ChangeUsernameResponse(newUsername.Value);
    }
}
