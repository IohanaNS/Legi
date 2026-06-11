namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user deletes a list. Social consumes this to purge the
/// list's <c>ContentSnapshot</c> and any associated <c>Like</c>, <c>Comment</c>,
/// and <c>ListFollow</c> rows.
/// </summary>
public sealed record UserListDeletedIntegrationEvent(
    Guid ListId,
    Guid OwnerId
) : IIntegrationEvent;
