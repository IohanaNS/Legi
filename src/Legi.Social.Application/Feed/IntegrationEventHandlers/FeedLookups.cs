using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Feed.IntegrationEventHandlers;

/// <summary>
/// The two local write-time lookups every feed-creation handler performs
/// (decisions 8.3 / 2.6.1): the actor's <see cref="UserProfile"/> for
/// username/avatar, and the <see cref="BookSnapshot"/> for title/author/cover.
///
/// Either missing is treated as transient — the upstream UserRegistered or
/// BookCreated event has not been consumed yet. The handler throws so the
/// dispatcher skips SaveChangesAsync and the broker redelivers (nack-and-requeue);
/// we never bake a FeedItem/ContentSnapshot with null actor or book data.
/// </summary>
internal static class FeedLookups
{
    public static async Task<UserProfile> GetProfileOrThrowAsync(
        IUserProfileRepository repository,
        Guid userId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            logger.LogWarning(
                "UserProfile lookup failed for user {UserId}; the UserRegistered event " +
                "has likely not been consumed yet. Throwing to redeliver.",
                userId);
            throw new InvalidOperationException(
                $"UserProfile for user {userId} not found; cannot build feed activity.");
        }

        return profile;
    }

    public static async Task<BookSnapshot> GetBookOrThrowAsync(
        IBookSnapshotRepository repository,
        Guid bookId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var book = await repository.GetByBookIdAsync(bookId, cancellationToken);
        if (book is null)
        {
            logger.LogWarning(
                "BookSnapshot lookup failed for book {BookId}; the BookCreated event " +
                "has likely not been consumed yet. Throwing to redeliver.",
                bookId);
            throw new InvalidOperationException(
                $"BookSnapshot for book {bookId} not found; cannot build feed activity.");
        }

        return book;
    }
}
