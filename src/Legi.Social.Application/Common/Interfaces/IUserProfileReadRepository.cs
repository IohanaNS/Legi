using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Common.Interfaces;

public interface IUserProfileReadRepository
{
    Task<IReadOnlyList<FollowUserDto>> SearchByUsernamePrefixAsync(
        string usernamePrefix,
        Guid? viewerUserId,
        int limit,
        CancellationToken cancellationToken = default);
}
