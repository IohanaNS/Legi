using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.ResendConfirmation;

public class ResendConfirmationCommandValidator : AbstractValidator<ResendConfirmationCommand>
{
    public ResendConfirmationCommandValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email or username is required")
            .MaximumLength(255).WithMessage("Email or username must be at most 255 characters");
    }
}
