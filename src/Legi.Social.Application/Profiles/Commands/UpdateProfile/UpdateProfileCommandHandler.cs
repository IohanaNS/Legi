using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Profiles.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(
    IUserProfileRepository userProfileRepository)
    : IRequestHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    public async Task<UpdateProfileResponse> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(request.UserId);
        if (profile is null)
            throw new NotFoundException(nameof(UserProfile), request.UserId);

        // Each method validates internally and updates UpdatedAt
        profile.UpdateBio(request.Bio);
        profile.UpdateAvatar(request.AvatarUrl);
        profile.UpdateBanner(request.BannerUrl);

        await userProfileRepository.UpdateAsync(profile);

        return new UpdateProfileResponse(
            profile.UserId,
            profile.Username,
            profile.Bio,
            profile.AvatarUrl,
            profile.BannerUrl,
            profile.UpdatedAt);
    }
}
