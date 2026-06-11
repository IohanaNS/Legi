namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user creates a custom book list. Social consumes this to
/// create a <c>ContentSnapshot</c> (List) ONLY when the list is public, making
/// the list interactable (likeable/commentable/followable). Private lists are
/// not projected, so the existing snapshot-existence guard in the like/comment
/// handlers rejects interactions on them automatically.
/// </summary>
public sealed record UserListCreatedIntegrationEvent(
    Guid ListId,
    Guid OwnerId,
    string Name,
    bool IsPublic
) : IIntegrationEvent;
