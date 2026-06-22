using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.DisableMfa;

public class DisableMfaCommandValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("A verification or recovery code is required to disable MFA");
    }
}
