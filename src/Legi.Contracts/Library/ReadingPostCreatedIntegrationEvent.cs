namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user creates a reading post (progress update + optional
/// content). Social consumes this to create both a <c>ContentSnapshot</c>
/// (Post) and a <c>FeedItem</c> (ProgressPosted).
///
/// <see cref="ProgressType"/> is the string form of
/// <c>Legi.Library.Domain.Enums.ProgressType</c> ("Page" or "Percentage"), or
/// null when no progress was supplied (content-only post).
///
/// Book display data is NOT carried — Social resolves it via its local
/// <c>BookSnapshot</c>. See decision 2.6.
/// </summary>
public sealed record ReadingPostCreatedIntegrationEvent(
    Guid PostId,
    Guid UserId,
    Guid BookId,
    string? Content,
    int? ProgressValue,
    string? ProgressType,
    DateTime CreatedAt,
    bool IsSpoiler = false
) : IIntegrationEvent;
