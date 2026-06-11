namespace Legi.Social.Application.Common.DTOs;

/// <summary>
/// A reference to a list the user follows. Carries only the list id and when it
/// was followed — the list's metadata (name, covers, owner) is owned by the
/// Library context and hydrated separately from <c>GET /library/lists/by-ids</c>.
/// </summary>
public record FollowedListDto(Guid ListId, DateTime FollowedAt);
