using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

/// <summary>
/// Raised when a user creates a list. Translated at the Application boundary
/// into a <c>UserListCreatedIntegrationEvent</c> for Social (list ContentSnapshot
/// when public).
/// </summary>
public sealed class UserListCreatedDomainEvent(
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
