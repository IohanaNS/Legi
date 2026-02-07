using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.Login;

public record LoginCommand(
    string EmailOrUsername,
    string Password
) : IRequest<LoginResponse>;