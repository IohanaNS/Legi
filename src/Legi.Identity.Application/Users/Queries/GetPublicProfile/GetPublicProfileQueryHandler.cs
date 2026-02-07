using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Users.Queries.GetPublicProfile;

public class GetPublicProfileQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetPublicProfileQuery, GetPublicProfileResponse>
{
    public async Task<GetPublicProfileResponse> Handle(
        GetPublicProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new ApplicationException("USER_NOT_FOUND");

        // Stats and IsFollowedByMe will come from Social Service (mock for now)
        var stats = new PublicUserStatsDto(0, 0, 0, 0);
        bool? isFollowedByMe = request.CurrentUserId.HasValue ? false : null;

        return new GetPublicProfileResponse(
            user.Id,
            user.Name,
            user.Bio,
            user.AvatarUrl,
            user.CreatedAt,
            stats,
            isFollowedByMe
        );
    }
}