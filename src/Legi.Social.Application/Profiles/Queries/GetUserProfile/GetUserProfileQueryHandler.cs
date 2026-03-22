using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Profiles.Queries.GetUserProfile;

public class GetUserProfileQueryHandler(
    IUserProfileRepository userProfileRepository,
    IFollowRepository followRepository)
    : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);
        if (profile is null)
            throw new NotFoundException(nameof(UserProfile), request.TargetUserId);

        var isFollowing = false;
        if (request.ViewerUserId.HasValue 
            && request.ViewerUserId.Value != request.TargetUserId)
        {
            var follow = await followRepository.GetByPairAsync(
                request.ViewerUserId.Value, request.TargetUserId, cancellationToken);
            isFollowing = follow is not null;
        }

        return new UserProfileDto
        {
            UserId = profile.UserId,
            Username = profile.Username,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            BannerUrl = profile.BannerUrl,
            FollowersCount = profile.FollowersCount,
            FollowingCount = profile.FollowingCount,
            IsFollowing = isFollowing,
            CreatedAt = profile.CreatedAt
        };
    }
}