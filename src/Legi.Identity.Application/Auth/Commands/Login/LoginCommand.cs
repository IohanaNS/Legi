namespace Legi.Identity.Application.Auth.Commands.Login;

using MediatR;

public record LoginCommand(
    string EmailOrUsername,
    string Password
) : IRequest<LoginResponse>;