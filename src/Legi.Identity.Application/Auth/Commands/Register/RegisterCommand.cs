using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.Register;

public record RegisterCommand(string Email, string Username, string Password) : IRequest<RegisterResponse>;
