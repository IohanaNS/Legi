namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user edits a list's details (name/description/visibility).
/// Social consumes this to keep the list <c>ContentSnapshot</c> in sync:
/// public → add/update the snapshot; private → delete it (which transparently
/// blocks further likes/comments/follows via the snapshot-existence guard).
/// </summary>
public sealed record UserListUpdatedIntegrationEvent(
    Guid ListId,
    Guid OwnerId,
    string Name,
    bool IsPublic
) : IIntegrationEvent;
