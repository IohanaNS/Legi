namespace Legi.Social.Application.Common.DTOs;

/// <summary>
/// The live social state of a list for a given viewer: interaction counts plus
/// the viewer's own like/follow flags. <see cref="IsInteractable"/> is false when
/// the list has no <see cref="Legi.Social.Domain.Entities.ContentSnapshot"/>
/// (i.e. it is private), in which case all counts/flags are zero/false.
/// </summary>
public record ListSocialStateDto(
    Guid ListId,
    bool IsInteractable,
    bool IsOwner,
    int LikesCount,
    int CommentsCount,
    int FollowersCount,
    bool IsLikedByMe,
    bool IsFollowedByMe
);
