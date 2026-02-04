using Legi.Identity.Application.Common.Mediator;

namespace Legi.Identity.Application.Auth.Commands.Logout;

public record LogoutCommand(
    Guid UserId,
    string RefreshToken
) : IRequest;