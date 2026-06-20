using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.GoogleSignIn;

public class GoogleSignInCommandValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("A Google credential is required.");
    }
}
