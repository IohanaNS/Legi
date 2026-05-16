namespace Legi.Contracts.Identity;

/// <summary>
/// Published when a user deletes their own account in the Identity service.
///
/// Consumers cascade the deletion across their bounded contexts:
/// <list type="bullet">
///   <item>Catalog: anonymizes <c>CreatedByUserId</c> on books the user added</item>
///   <item>Library: hard-deletes the user's reading history (user_books,
///         reading_posts, user_lists)</item>
///   <item>Social: hard-deletes the user's profile, follows, likes, comments,
///         feed items, and content snapshots</item>
/// </list>
///
/// Each consumer is idempotent — running the cascade twice produces the same
/// final state as once.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, section 6.2.
/// </summary>
/// <param name="UserId">Identity's user identifier; same UUID used by all consumers.</param>
/// <param name="DeletedAt">UTC timestamp at which the account was deleted.</param>
public sealed record UserDeletedIntegrationEvent(
    Guid UserId,
    DateTime DeletedAt
) : IIntegrationEvent;
