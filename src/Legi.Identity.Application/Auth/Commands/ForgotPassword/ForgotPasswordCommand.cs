using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(
    string Email,
    string? TurnstileToken = null,
    string? RemoteIpAddress = null,
    string? Language = null
) : IRequest<Unit>;
