using Legi.Social.Application.Common.DTOs;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Common.Interfaces;

public interface ICommentReadRepository
{
    /// <summary>
    /// Gets comments on a piece of content, ordered by CreatedAt DESC.
    /// Joins with user_profiles for commenter username/avatar.
    /// </summary>
    Task<PaginatedList<CommentDto>> GetByTargetAsync(
        InteractableType targetType,
        Guid targetId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}