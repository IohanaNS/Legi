using Legi.Contracts.Identity;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.Books.IntegrationEventHandlers;

/// <summary>
/// Library's consumer for <see cref="UserDeletedIntegrationEvent"/>. Hard-deletes
/// every piece of user-owned data in Library: user_books (including soft-deleted
/// rows), user_lists (cascading to user_list_items), and reading_posts.
///
/// Hard delete is intentional: soft-delete exists for user-facing recoverability
/// (a user removing a book from their library can re-add it), but the moment
/// the user themselves is gone, there's no one for whom preservation matters.
/// GDPR-style erasure also implies hard delete on account deletion.
///
/// Uses bulk SQL deletes via <c>ExecuteDeleteAsync</c>. Bypasses the change
/// tracker and the domain-event mechanism (no <c>BookRemovedFromLibraryDomainEvent</c>
/// per-row fanout) — for <c>UserDeleted</c> this is desired, since Social has
/// its own direct subscription to <c>UserDeletedIntegrationEvent</c> and doesn't
/// need Library to relay anything.
///
/// Idempotent as a whole: each individual delete matches zero rows on rerun.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, section 8.1 (bulk operations
/// exception) and section 6.2 (UserDeleted cascade table).
///
/// MUST NOT call SaveChangesAsync — see decision 8.1.
/// </summary>
public sealed class UserDeletedIntegrationEventHandler(
    IUserBookRepository userBookRepository,
    IReadingPostRepository readingPostRepository,
    IUserListRepository userListRepository,
    ILogger<UserDeletedIntegrationEventHandler> logger)
    : INotificationHandler<UserDeletedIntegrationEvent>
{
    public async Task Handle(
        UserDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var userId = integrationEvent.UserId;

        var readingPostsDeleted = await readingPostRepository
            .DeleteAllForUserAsync(userId, cancellationToken);

        var userListsDeleted = await userListRepository
            .DeleteAllForUserAsync(userId, cancellationToken);

        var userBooksDeleted = await userBookRepository
            .DeleteAllForUserAsync(userId, cancellationToken);

        logger.LogInformation(
            "Deleted Library data for user {UserId}: " +
            "{UserBooksCount} user_book(s), {UserListsCount} user_list(s), {ReadingPostsCount} reading_post(s)",
            userId, userBooksDeleted, userListsDeleted, readingPostsDeleted);
    }
}
