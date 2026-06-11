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
}
