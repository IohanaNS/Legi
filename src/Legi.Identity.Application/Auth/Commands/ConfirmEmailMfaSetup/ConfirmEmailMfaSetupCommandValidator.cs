using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.ConfirmEmailMfaSetup;

public class ConfirmEmailMfaSetupCommandValidator : AbstractValidator<ConfirmEmailMfaSetupCommand>
{
    public ConfirmEmailMfaSetupCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required")
            .Matches(@"^\d{6}$").WithMessage("Verification code must be 6 digits");
    }
}
