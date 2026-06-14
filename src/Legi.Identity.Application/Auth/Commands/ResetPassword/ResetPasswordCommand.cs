using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Token,
    string NewPassword
) : IRequest<Unit>;
