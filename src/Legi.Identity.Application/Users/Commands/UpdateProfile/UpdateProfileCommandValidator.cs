using FluentValidation;

namespace Legi.Identity.Application.Users.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .MinimumLength(2).WithMessage("Name must be at least 2 characters")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters");
        });

        When(x => x.Bio is not null, () =>
        {
            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio must be at most 500 characters");
        });

        When(x => x.AvatarUrl is not null, () =>
        {
            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500).WithMessage("Avatar URL must be at most 500 characters");
        });
    }
}