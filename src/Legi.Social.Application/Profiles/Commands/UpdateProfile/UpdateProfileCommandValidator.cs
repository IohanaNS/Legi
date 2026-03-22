using FluentValidation;
using Legi.Social.Domain.Entities;

namespace Legi.Social.Application.Profiles.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Bio)
            .MaximumLength(UserProfile.MaxBioLength)
            .When(x => x.Bio is not null);
    }
}
