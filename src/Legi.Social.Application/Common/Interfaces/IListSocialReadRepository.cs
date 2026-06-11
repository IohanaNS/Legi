using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Common.Interfaces;

public interface IListSocialReadRepository
{
    /// <summary>
    /// Computes the live social state of a list (counts + the viewer's like/follow
    /// flags). When the list has no ContentSnapshot (it is private), returns a
    /// non-interactable state with zeroed counts.
    /// </summary>
    Task<ListSocialStateDto> GetStateAsync(
        Guid listId,
        Guid? viewerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the lists the given user follows (most recently followed first),
    /// paginated. Only the list ids + follow timestamps are returned; the list
    /// metadata is hydrated from the Library context by the caller.
    /// </summary>
    Task<PaginatedList<FollowedListDto>> GetFollowedListsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
