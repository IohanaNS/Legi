namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user adds a book to their library. Social consumes this to
/// create a <c>FeedItem</c> (BookStarted) when <see cref="Wishlist"/> is false.
///
/// Book display data (title, authors, cover) is intentionally NOT carried —
/// Social resolves it via its local <c>BookSnapshot</c> read model. See
/// MESSAGING-ARCHITECTURE-decisions.md, decision 2.6.
/// </summary>
public sealed record BookAddedToLibraryIntegrationEvent(
    Guid UserBookId,
    Guid UserId,
    Guid BookId,
    bool Wishlist,
    DateTime AddedAt
) : IIntegrationEvent;
