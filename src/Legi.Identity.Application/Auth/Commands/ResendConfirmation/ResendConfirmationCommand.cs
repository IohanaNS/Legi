using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ResendConfirmation;

public record ResendConfirmationCommand(
    string EmailOrUsername,
    string? TurnstileToken = null,
    string? RemoteIpAddress = null,
    string? Language = null
) : IRequest<Unit>;
