namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user deletes a reading post. Social consumes this to purge
/// the <c>ContentSnapshot</c>, <c>FeedItem</c>, and any associated
/// <c>Like</c>/<c>Comment</c> rows.
/// </summary>
public sealed record ReadingPostDeletedIntegrationEvent(
    Guid PostId,
    Guid UserId
) : IIntegrationEvent;
