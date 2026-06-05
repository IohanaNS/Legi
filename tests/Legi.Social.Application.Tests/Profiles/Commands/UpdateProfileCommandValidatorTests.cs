using Legi.Social.Application.Profiles.Commands.UpdateProfile;
using Legi.Social.Domain.Entities;

namespace Legi.Social.Application.Tests.Profiles.Commands;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_BioIsNull_Passes()
    {
        var command = new UpdateProfileCommand(Guid.NewGuid(), null, null, null);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BioExceedsMaximumLength_Fails()
    {
        var command = new UpdateProfileCommand(
            Guid.NewGuid(),
            new string('a', UserProfile.MaxBioLength + 1),
            null,
            null);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.Bio));
    }
}
