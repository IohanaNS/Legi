using MediatR;

namespace Legi.Identity.Application.Auth.Commands.Logout;

public record LogoutCommand(
    Guid UserId,
    string RefreshToken
) : IRequest;