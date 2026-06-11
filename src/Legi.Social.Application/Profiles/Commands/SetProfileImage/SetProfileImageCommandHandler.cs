using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Common.Storage;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Profiles.Commands.SetProfileImage;

public class SetProfileImageCommandHandler(
    IUserProfileRepository userProfileRepository,
    IObjectStorage objectStorage)
    : IRequestHandler<SetProfileImageCommand, SetProfileImageResponse>
{
    public async Task<SetProfileImageResponse> Handle(
        SetProfileImageCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(request.UserId);
        if (profile is null)
            throw new NotFoundException(nameof(UserProfile), request.UserId);

        var previousUrl = request.Kind == ProfileImageKind.Avatar
            ? profile.AvatarUrl
            : profile.BannerUrl;

        if (request.Kind == ProfileImageKind.Avatar)
            profile.UpdateAvatar(request.Url);
        else
            profile.UpdateBanner(request.Url);

        await userProfileRepository.UpdateAsync(profile);

        // Best-effort cleanup of the replaced object — the new URL is already
        // persisted, so a failed delete only leaves an orphan, never a dangling
        // profile reference.
        if (!string.IsNullOrEmpty(previousUrl) && previousUrl != request.Url)
            await objectStorage.DeleteByUrlAsync(previousUrl, cancellationToken);

        return new SetProfileImageResponse(request.Url);
    }
}
