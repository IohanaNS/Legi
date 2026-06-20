using Legi.Identity.Application.Auth.Commands.Login;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.GoogleSignIn;

public record GoogleSignInCommand(string IdToken, string? RemoteIpAddress)
    : IRequest<LoginResponse>;
