using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<RefreshTokenResponse>;