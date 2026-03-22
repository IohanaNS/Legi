using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Common.Interfaces;

public interface IFollowReadRepository
{
    /// <summary>
    /// Gets the followers of a user with profile info and contextual IsFollowedByViewer flag.
    /// ViewerUserId is optional (anonymous viewing returns IsFollowedByViewer = false).
    /// </summary>
    Task<PaginatedList<FollowUserDto>> GetFollowersAsync(
        Guid userId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the users that a user follows, with the same contextual flag.
    /// </summary>
    Task<PaginatedList<FollowUserDto>> GetFollowingAsync(
        Guid userId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}