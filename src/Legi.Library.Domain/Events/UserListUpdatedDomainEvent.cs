using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

/// <summary>
/// Raised when a user edits a list's details (name/description/visibility).
/// Translated at the Application boundary into a
/// <c>UserListUpdatedIntegrationEvent</c> for Social (keeps the list
/// ContentSnapshot in sync with the current visibility).
/// </summary>
public sealed class UserListUpdatedDomainEvent(
    Guid userListId,
    Guid userId,
    string name,
    bool isPublic) : IDomainEvent
{
    public Guid UserListId { get; } = userListId;
    public Guid UserId { get; } = userId;
    public string Name { get; } = name;
    public bool IsPublic { get; } = isPublic;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
