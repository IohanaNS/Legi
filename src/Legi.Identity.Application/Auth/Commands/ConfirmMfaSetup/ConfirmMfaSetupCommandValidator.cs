using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.ConfirmMfaSetup;

public class ConfirmMfaSetupCommandValidator : AbstractValidator<ConfirmMfaSetupCommand>
{
    public ConfirmMfaSetupCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required")
            .Matches(@"^\d{6}$").WithMessage("Verification code must be 6 digits");
    }
}
