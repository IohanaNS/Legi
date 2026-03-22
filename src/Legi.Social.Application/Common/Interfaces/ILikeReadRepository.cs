using Legi.Social.Application.Common.DTOs;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Common.Interfaces;

public interface ILikeReadRepository
{
    /// <summary>
    /// Gets users who liked a piece of content, ordered by CreatedAt DESC.
    /// Joins with user_profiles for liker info and contextual IsFollowedByViewer flag.
    /// ViewerUserId is optional (anonymous returns IsFollowedByViewer = false).
    /// </summary>
    Task<PaginatedList<LikeUserDto>> GetByTargetAsync(
        InteractableType targetType,
        Guid targetId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}