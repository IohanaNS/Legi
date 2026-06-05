using Legi.Contracts.Catalog;
using Legi.Contracts.Identity;
using Legi.Contracts.Social;

namespace Legi.Library.Application.Tests.Factories;

public static class CatalogIntegrationEventFactory
{
    public static BookCreatedIntegrationEvent BookCreated(
        Guid? bookId = null,
        string isbn = "9780132350884",
        string title = "Clean Code",
        List<string>? authors = null,
        string? coverUrl = "https://example.com/clean-code.jpg",
        int? pageCount = 464)
    {
        return new BookCreatedIntegrationEvent(
            bookId ?? LibraryTestIds.BookId,
            isbn,
            title,
            authors ?? ["Robert C. Martin"],
            coverUrl,
            pageCount);
    }

    public static BookUpdatedIntegrationEvent BookUpdated(
        Guid? bookId = null,
        string isbn = "9780132350884",
        string title = "Clean Code",
        List<string>? authors = null,
        string? coverUrl = "https://example.com/clean-code.jpg",
        int? pageCount = 464)
    {
        return new BookUpdatedIntegrationEvent(
            bookId ?? LibraryTestIds.BookId,
            isbn,
            title,
            authors ?? ["Robert C. Martin"],
            coverUrl,
            pageCount);
    }
}

public static class IdentityIntegrationEventFactory
{
    public static UserDeletedIntegrationEvent UserDeleted(
        Guid? userId = null,
        DateTime? deletedAt = null)
    {
        return new UserDeletedIntegrationEvent(
            userId ?? LibraryTestIds.UserId,
            deletedAt ?? new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
    }
}

public static class SocialIntegrationEventFactory
{
    public static ContentLikedIntegrationEvent ContentLiked(
        string targetType = "Post",
        Guid? targetId = null,
        Guid? userId = null)
    {
        return new ContentLikedIntegrationEvent(
            targetType,
            targetId ?? LibraryTestIds.ReadingPostId,
            userId ?? LibraryTestIds.UserId);
    }

    public static ContentUnlikedIntegrationEvent ContentUnliked(
        string targetType = "Post",
        Guid? targetId = null,
        Guid? userId = null)
    {
        return new ContentUnlikedIntegrationEvent(
            targetType,
            targetId ?? LibraryTestIds.ReadingPostId,
            userId ?? LibraryTestIds.UserId);
    }

    public static ContentCommentedIntegrationEvent ContentCommented(
        string targetType = "Post",
        Guid? targetId = null,
        Guid? commentId = null,
        Guid? userId = null)
    {
        return new ContentCommentedIntegrationEvent(
            targetType,
            targetId ?? LibraryTestIds.ReadingPostId,
            commentId ?? Guid.Parse("66666666-6666-6666-6666-666666666666"),
            userId ?? LibraryTestIds.UserId);
    }

    public static CommentDeletedIntegrationEvent CommentDeleted(
        string targetType = "Post",
        Guid? targetId = null,
        Guid? commentId = null,
        Guid? userId = null)
    {
        return new CommentDeletedIntegrationEvent(
            targetType,
            targetId ?? LibraryTestIds.ReadingPostId,
            commentId ?? Guid.Parse("66666666-6666-6666-6666-666666666666"),
            userId ?? LibraryTestIds.UserId);
    }
}
