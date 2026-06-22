using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.CompleteMfaLogin;

public class CompleteMfaLoginCommandValidator : AbstractValidator<CompleteMfaLoginCommand>
{
    public CompleteMfaLoginCommandValidator()
    {
        RuleFor(x => x.MfaToken).NotEmpty().WithMessage("MFA challenge token is required");
        RuleFor(x => x.Code).NotEmpty().WithMessage("A verification or recovery code is required");
    }
}
