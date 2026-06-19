using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(string Token) : IRequest<Unit>;
